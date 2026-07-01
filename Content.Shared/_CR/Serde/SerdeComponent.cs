using Robust.Shared.GameObjects;
using Robust.Shared.GameStates; // Required for AutoGenerateComponentState
using Robust.Shared.Serialization;


namespace Content.Shared._CR.Serde;

// <summary>
// Main component of serde, communicates with the message broker
// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SerdeComponent : Component
{
    [DataField]
    public bool DebugLogging = false;

    // tells entities connected to serde what type of thing it is
    // this is mostly for communicating the prototype's intentions to external programs
    [DataField]
    public string ActorTags = "npc;npc_genaric";

    // disables most functionality while player takes over the mob
    // the player unless consenting should always overpower Serde commands
    // nor should they generate out events.
    // plugins should read the AcceptingCommands field before acting on an object
    // plugins should be responsable for sending paused and resumed out messages
    // when they set AcceptingCommands
    [DataField]
    public bool DisableOnTakeover = true;

    [DataField]
    public bool EnableOnRelease = true;

    [DataField]
    public bool AcceptingCommands = true;
}
