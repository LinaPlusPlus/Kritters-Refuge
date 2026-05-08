namespace Content.Shared.Humanoid;

/// <summary>
/// Raised when a humanoid's effective height is changed.
/// </summary>
public readonly record struct HumanoidHeightChangedEvent(float Height);
