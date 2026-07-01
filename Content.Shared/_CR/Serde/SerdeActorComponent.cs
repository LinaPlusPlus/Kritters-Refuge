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
    [DataField]
    public bool CanSpeak = true;

    [DataField]
    public bool CanListen = true;
}
