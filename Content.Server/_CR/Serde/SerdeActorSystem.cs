using Content.Server.Speech;
using Content.Server.Chat.Systems;

using Content.Shared._CR.Serde;
namespace Content.Server._CR.Serde;

public sealed class SerdeActorSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    // Always subscribe to events here, on initialize
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("crSerde.logs");

        SubscribeLocalEvent<SerdeActorComponent, ComponentInit>(OnActorInit);
        SubscribeLocalEvent<SerdeActorComponent, SerdeInEvent>(OnSerdeIn);
        SubscribeLocalEvent<SerdeActorComponent, ListenEvent>(OnListen);
        //TODO: somthings broken with the activeListener component
    }

    private void OnActorInit(Entity<SerdeActorComponent> ent, ref ComponentInit _)
    {
        RaiseLocalEvent(ent, new SerdeOutEvent(0, "gainedCapability", "Actor", 0, 0, 0));
    }

    private void OnListen(Entity<SerdeActorComponent> ent, ref ListenEvent args)
    {
        if (!ent.Comp.CanListen) return;

        // if it's okay to be accepting commands
        EntityManager.EnsureComponent<SerdeComponent>(ent, out var serdeComponent);
        if (!serdeComponent.AcceptingCommands) return;

        var message = args.Message.Trim();
        var source = args.Source;

        RaiseLocalEvent(ent, new SerdeOutEvent(0, "heard", message, (int)source, 0, 0));
    }

    private void OnSerdeIn(Entity<SerdeActorComponent> ent, ref SerdeInEvent sev)
    {
        // if it's okay to be accepting commands
        EntityManager.EnsureComponent<SerdeComponent>(ent, out var serdeComponent);
        if (!serdeComponent.AcceptingCommands) return;

        if (sev.Command == "say")
        {
            if (!ent.Comp.CanSpeak) return;
            _chat.TrySendInGameICMessage(ent.Owner, sev.Text, InGameICChatType.Speak, ChatTransmitRange.Normal, false);
            return;
        }

    }
}
