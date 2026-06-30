using Robust.Shared.GameStates;


namespace Content.Shared._CR.Serde;

// <summary>
// Component to debug the event serde system
// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SerdeComponent : Component
{
    // Data field editable via YAML prototypes
    // safety is super important, only debug should be able to enable verbose logging
    [DataField("debugLogging"), ViewVariables(VVAccess.ReadOnly)]
    public bool DebugLogging = false;

    // <summary>
    // disables most functionality while player enhabits the mob
    // the player unless rather kinky should always overpower Serde commands
    // disabling this flag should be a `High` or `Severe` level log
    // </summary>
    [DataField("playerSafety"), ViewVariables(VVAccess.ReadOnly)]
    public bool PlayerSafety = true;
}
