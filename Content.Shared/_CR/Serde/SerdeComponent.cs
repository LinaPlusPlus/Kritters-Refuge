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
    // Data field editable via YAML prototypes
    // safety is super important, only debug privlages should be able to enable verbose logging
    [DataField("debugLogging"), ViewVariables(VVAccess.ReadOnly)]
    public bool DebugLogging = false;

    // tells entities connected to serde what type of thing it is
    // this is mostly for communicating the prototype's intentions to external programs
    [DataField("actorClass"), ViewVariables(VVAccess.ReadWrite)]
    public string actorClass = "npc_genaric";
}
