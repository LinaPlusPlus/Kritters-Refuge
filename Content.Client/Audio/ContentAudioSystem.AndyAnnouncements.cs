using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    private static readonly ISawmill Sawmill = Logger.GetSawmill("andy.audio");

    private const string PocketSizedAndyFolderSegment = "/PocketSizedAndy/";
    private const string AndyAnnouncementFallbackPath = "/Audio/Announcements/announce.ogg";
    private const float AndyAnnouncementMaxVolume = 0f;
    // Options sliders display whole percents. Treat <= 1% as muted so "0%" cannot leak quiet playback.
    private const float AndyAnnouncementMuteThreshold = 0.01f;

    private float _andyAnnouncementVolume;
    private bool _andyAnnouncementsEnabled = true;
    private bool _andyAnnouncementsMuted;
    private readonly Dictionary<EntityUid, float> _andyAnnouncementBaseVolumes = new();
    private readonly HashSet<EntityUid> _andyAnnouncementFallbackPlayed = new();
    private readonly HashSet<EntityUid> _andyAnnouncementDisabledHandled = new();
    private bool _andyAnnouncementsInitialized;
    private bool _andyAnnouncementsDebug;

    private void InitializeAndyAnnouncements()
    {
        if (_andyAnnouncementsInitialized)
            return;

        _andyAnnouncementsInitialized = true;

        // Prime from current config immediately so startup audio cannot use stale defaults.
        _andyAnnouncementsEnabled = _configManager.GetCVar(CCVars.AndyAnnouncementsEnabled);
        var currentVolume = _configManager.GetCVar(CCVars.AndyAnnouncementVolume);
        _andyAnnouncementsMuted = currentVolume <= AndyAnnouncementMuteThreshold;
        _andyAnnouncementVolume = SharedAudioSystem.GainToVolume(currentVolume);
        _andyAnnouncementsDebug = _configManager.GetCVar(CCVars.AndyAnnouncementsDebug);

        Subs.CVar(_configManager, CCVars.AndyAnnouncementsEnabled, AndyAnnouncementEnabledChanged, true);
        Subs.CVar(_configManager, CCVars.AndyAnnouncementVolume, AndyAnnouncementVolumeChanged, true);
        Subs.CVar(_configManager, CCVars.AndyAnnouncementsDebug, AndyAnnouncementDebugChanged, true);
        TrySubscribeAndyAudioEvents();

        DebugAndy($"Initialized: enabled={_andyAnnouncementsEnabled}, muted={_andyAnnouncementsMuted}, volume={_andyAnnouncementVolume:0.###}");
    }

    private void TrySubscribeAndyAudioEvents()
    {
        try
        {
            SubscribeLocalEvent<AudioComponent, ComponentStartup>(OnAudioStartup);
            SubscribeLocalEvent<AudioComponent, ComponentShutdown>(OnAudioShutdown);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("Duplicate Subscriptions", StringComparison.Ordinal))
        {
            // Can happen when reconnecting without a full process restart.
        }
    }

    private void OnAudioStartup(EntityUid uid, AudioComponent component, ComponentStartup args)
    {
        if (_andyAnnouncementsDebug && ShouldTraceFileName(component.FileName))
            DebugAndy($"Audio startup uid={uid} file={component.FileName}");

        // Apply toggle/volume immediately so short one-shot announcements can't slip through.
        UpdateAndyAnnouncementVolume(uid, component);
    }

    private void OnAudioShutdown(EntityUid uid, AudioComponent component, ComponentShutdown args)
    {
        _andyAnnouncementBaseVolumes.Remove(uid);
        _andyAnnouncementFallbackPlayed.Remove(uid);
        _andyAnnouncementDisabledHandled.Remove(uid);
    }

    private void AndyAnnouncementEnabledChanged(bool enabled)
    {
        _andyAnnouncementsEnabled = enabled;

        if (enabled)
        {
            _andyAnnouncementDisabledHandled.Clear();
            _andyAnnouncementFallbackPlayed.Clear();
        }

        UpdateAndyAnnouncementVolumes();
    }

    private void AndyAnnouncementVolumeChanged(float volume)
    {
        _andyAnnouncementsMuted = volume <= AndyAnnouncementMuteThreshold;
        _andyAnnouncementVolume = SharedAudioSystem.GainToVolume(volume);
        DebugAndy($"Volume cvar changed: gain={volume:0.###}, muted={_andyAnnouncementsMuted}, db={_andyAnnouncementVolume:0.###}");

        UpdateAndyAnnouncementVolumes();
    }

    private void AndyAnnouncementDebugChanged(bool enabled)
    {
        _andyAnnouncementsDebug = enabled;
        DebugAndy($"Debug tracing {(enabled ? "enabled" : "disabled")}");
    }

    private void UpdateAndyAnnouncementVolumes()
    {
        var snapshot = new List<EntityUid>();
        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            snapshot.Add(uid);
        }

        foreach (var uid in snapshot)
        {
            if (!TryComp(uid, out AudioComponent? component))
                continue;

            UpdateAndyAnnouncementVolume(uid, component);
        }
    }

    private void UpdateAndyAnnouncementVolume(EntityUid uid, AudioComponent component)
    {
        var isAndy = IsAndyAnnouncement(component.FileName);
        if (!isAndy)
            return;

        if (!_andyAnnouncementsEnabled && _andyAnnouncementDisabledHandled.Contains(uid))
            return;

        if (_andyAnnouncementsDebug && (isAndy || ShouldTraceFileName(component.FileName)))
        {
            DebugAndy($"Evaluate uid={uid} file={component.FileName} isAndy={isAndy} enabled={_andyAnnouncementsEnabled} muted={_andyAnnouncementsMuted} playing={component.Playing}");
        }

        if (!_andyAnnouncementsEnabled)
        {
            _andyAnnouncementDisabledHandled.Add(uid);

            if (_andyAnnouncementFallbackPlayed.Add(uid))
            {
                DebugAndy($"Replacing Andy clip with fallback uid={uid} fallback={AndyAnnouncementFallbackPath}");
                _audio.PlayGlobal(new ResolvedPathSpecifier(AndyAnnouncementFallbackPath), Filter.Local(), false, component.Params);
            }

            // Stop the original Andy clip so only the replacement announcement is heard.
            _audio.Stop(uid, component);
            DebugAndy($"Stopped Andy clip due to disabled toggle uid={uid}");
            return;
        }

        if (_andyAnnouncementsMuted)
        {
            _audio.Stop(uid, component);
            DebugAndy($"Stopped Andy clip due to muted slider uid={uid}");
            return;
        }

        if (!_andyAnnouncementBaseVolumes.TryGetValue(uid, out var baseVolume))
        {
            baseVolume = component.Params.Volume;
            _andyAnnouncementBaseVolumes[uid] = baseVolume;
        }

        var expected = MathF.Min(baseVolume + _andyAnnouncementVolume, AndyAnnouncementMaxVolume);

        if (MathF.Abs(component.Volume - expected) < 0.001f)
            return;

        _audio.SetVolume(uid, expected, component);
        DebugAndy($"Adjusted Andy volume uid={uid} expected={expected:0.###}");
    }

    private void DebugAndy(string message)
    {
        if (!_andyAnnouncementsDebug)
            return;

        Sawmill.Info($"[ContentAudioSystem] {message}");
    }

    private static bool ShouldTraceFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return fileName.Contains("andy", StringComparison.OrdinalIgnoreCase)
               || fileName.Contains("announce", StringComparison.OrdinalIgnoreCase)
               || fileName.Contains("PocketSizedAndy", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAndyAnnouncement(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var normalized = fileName.Replace('\\', '/').Trim();

        if (normalized.StartsWith("SoundPathSpecifier(", StringComparison.OrdinalIgnoreCase)
            && normalized.EndsWith(")", StringComparison.Ordinal))
        {
            normalized = normalized["SoundPathSpecifier(".Length..^1];
        }

        if (normalized.StartsWith("ResolvedPathSpecifier(", StringComparison.OrdinalIgnoreCase)
            && normalized.EndsWith(")", StringComparison.Ordinal))
        {
            normalized = normalized["ResolvedPathSpecifier(".Length..^1];
        }

        normalized = normalized.Trim();
        if (!normalized.StartsWith('/'))
            normalized = $"/{normalized}";

        if (normalized.EndsWith(")", StringComparison.Ordinal))
            normalized = normalized[..^1];

        // Match any resource path that plays from a PocketSizedAndy folder
        // (e.g. /Audio/_NF/Announcements/PocketSizedAndy/* or /Audio/_NF/Ambience/PocketSizedAndy/*).
        if (!normalized.Contains(PocketSizedAndyFolderSegment, StringComparison.OrdinalIgnoreCase))
            return false;

        return normalized.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
    }
}