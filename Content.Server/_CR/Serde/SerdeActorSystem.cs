using Content.Server.Speech;
using Content.Server.Speech.Components;

using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;

namespace Content.Server._CR.Serde;
using Content.Shared._CR.Serde;

// A system running on the server...
// Whenever an user interacts with an entity that has this component,
// a counter in it will be incremented by zero. An event will be raised too.
// Other Entity Systems can interact with FooComponent using the public API here.
public sealed class SerdeActorSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    // Always subscribe to events here, on initialize
    public override void Initialize()
    {
        base.Initialize();

        // "my_system.debug" is the category name for the logs
        _sawmill = _logManager.GetSawmill("crSerde.logs");

        // Log a debug message
        //_sawmill.Debug("System successfully initialized and ready for testing.");

        // Log a warning message
        //_sawmill.Warning("A minor issue was detected, proceeding anyway.");

        // Subscribe to FooComponent being initialized...
        SubscribeLocalEvent<SerdeActorComponent, ComponentInit>(OnActorInit);
        SubscribeLocalEvent<SerdeActorComponent, SerdeInEvent>(OnSerdeIn);
        SubscribeLocalEvent<SerdeActorComponent, ListenEvent>(OnListen);

        // Subscribe to FooComponent being interacted on by an user with an item.
        //SubscribeLocalEvent<SerdeComponent, InteractUsingEvent>(Handle);

        // Subscribe to the MoveEvent broadcast event, raised whenever
        // an entity moves... Just an example subscription
        // SubscribeLocalEvent<MoveEvent>(OnEntityMove);
    }

    // This is called when a FooComponent is initialized.
    private void OnActorInit(Entity<SerdeActorComponent> ent, ref ComponentInit _)
    {

    }

    private void OnListen(Entity<SerdeActorComponent> ent, ref ListenEvent args)
    {
        var component = ent.Comp;
        var message = args.Message.Trim();
        var source = args.Source;

        RaiseLocalEvent(ent, new SerdeOutEvent(0,"heard",message,(int) source,0,0));
    }

    private void OnSerdeIn(Entity<SerdeActorComponent> ent, ref SerdeInEvent sev)
    {
        // sets if it's okay to be accepting commands
        bool safe = true;

        if (!safe) return;

        if (sev.Command == "say") {
            _chat.TrySendInGameICMessage(ent.Owner, sev.Text, InGameICChatType.Speak, ChatTransmitRange.Normal, false);
            return;
        }

    }
}
