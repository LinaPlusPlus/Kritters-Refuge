namespace Content.Shared._Goobstation.Vehicles;

/// <summary>
/// Ejects a vehicle's driver when colliding above a configured speed.
/// </summary>
[RegisterComponent]
public sealed partial class VehicleImpactEjectComponent : Component
{
    [DataField("minimumCollisionSpeed")]
    public float MinimumCollisionSpeed = 10f;

    [DataField("knockdownSeconds")]
    public float KnockdownSeconds = 2f;
}
