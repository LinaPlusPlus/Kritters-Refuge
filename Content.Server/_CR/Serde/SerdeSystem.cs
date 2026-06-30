namespace Content.Server._CR.Serde;
using Content.Shared._CR.Serde;

using Robust.Shared.Serialization;
// A system running on the server...
// Whenever an user interacts with an entity that has this component,
// a counter in it will be incremented by zero. An event will be raised too.
// Other Entity Systems can interact with FooComponent using the public API here.
public sealed class SerdeSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;

    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    // Always subscribe to events here, on initialize
    public override void Initialize()
    {
        base.Initialize();

        // "my_system.debug" is the category name for the logs
        _sawmill = _logManager.GetSawmill("cr_serde.debug");

        // Log a debug message
        //_sawmill.Debug("System successfully initialized and ready for testing.");

        // Log a warning message
        //_sawmill.Warning("A minor issue was detected, proceeding anyway.");

        // Subscribe to FooComponent being initialized...
        SubscribeLocalEvent<SerdeComponent, ComponentInit>(OnSerdeInit);
        SubscribeLocalEvent<SerdeComponent, SerdeInEvent>(OnSerdeIn);
        SubscribeLocalEvent<SerdeComponent, SerdeOutEvent>(OnSerdeOut);

        // Subscribe to FooComponent being interacted on by an user with an item.
        //SubscribeLocalEvent<SerdeComponent, InteractUsingEvent>(Handle);

        // Subscribe to the MoveEvent broadcast event, raised whenever
        // an entity moves... Just an example subscription
        // SubscribeLocalEvent<MoveEvent>(OnEntityMove);
    }

    // This is called when a FooComponent is initialized.
    private void OnSerdeInit(Entity<SerdeComponent> ent, ref ComponentInit _)
    {
         // Initialize your FooComponent here
    }

    private void OnSerdeIn(Entity<SerdeComponent> ent, ref SerdeInEvent sev)
    {

        if (ent.Comp.DebugLogging)
        {
            _sawmill.Info($"Serde in: {sev.Command} '{sev.Text}' {sev.A} {sev.X} {sev.Y}");
        }
    }

    private void OnSerdeOut(Entity<SerdeComponent> ent, ref SerdeOutEvent sev)
    {
        if (ent.Comp.DebugLogging)
        {
            // I am not c# skilled enough to compress this line
            _sawmill.Info($"Serde out:  {sev.Command} '{sev.Text}' {sev.A} {sev.X} {sev.Y}");
        }
    }

    public void CommandRaiseIn(
        Entity<SerdeComponent?> ent,
        //SerdeInEvent inEvent,
        string command, string text,
        int a, float x, float y
    )
    {
        RaiseLocalEvent(ent, new SerdeInEvent(command, text, a, x, y));
    }
}


public sealed class SerdeInEvent : EntityEventArgs
{
    public string Command { get; }
    public string Text { get; }
    public int A { get; }
    public float X { get; }
    public float Y { get; }

    public SerdeInEvent(string command, string text, int a, float x, float y)
    {
        Command = command;
        Text = text;
        A = a;
        X = x;
        Y = y;
    }
}

public sealed class SerdeOutEvent : EntityEventArgs
{
    public string Command { get; }
    public string Text { get; }
    public int A { get; }
    public float X { get; }
    public float Y { get; }

    public SerdeOutEvent(string command, string text, int a, float x, float y)
    {
        Command = command;
        Text = text;
        A = a;
        X = x;
        Y = y;
    }
}
