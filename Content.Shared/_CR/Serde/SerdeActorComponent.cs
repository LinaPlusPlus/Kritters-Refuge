namespace Content.Shared._CR.Serde;

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates; // Required for AutoGenerateComponentState
using Robust.Shared.Serialization;

// <summary>
// Component to debug the event serde system
// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SerdeActorComponent : Component
{
    // <summary>
    // disables most functionality while player enhabits the mob
    // the player unless rather kinky should always overpower Serde commands
    // disabling this flag should be a `High` or `Severe` level log
    // </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool PlayerSafety = true;

    [DataField]
    public bool CanSpeak = true;

    [DataField]
    public bool CanListen = true;
}
