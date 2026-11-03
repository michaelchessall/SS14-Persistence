using Content.Shared._Persistence14.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Persistence14.Weapons.Ranged.Components;

/// <summary>
/// Overrides the zero gravity spread penalty for this gun. Guns without this component use the defaults below.
/// The penalty applies while the gun is held by someone weightless (magboots prevent it).
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunPenaltySystem))]
public sealed partial class ZeroGravityGunPenaltyComponent : Component
{
    public const float DefaultAngleMultiplier = 2f;
    public const double DefaultFlatAngleDegrees = 8;
    public const double DefaultMaxAngleDegrees = 90;

    /// <summary>
    /// Multiplies min/max spread and recoil buildup, and divides recoil recovery, while weightless.
    /// Set to 1 along with <see cref="FlatAngle"/> 0 to make a gun ignore zero gravity.
    /// </summary>
    [DataField]
    public float AngleMultiplier = DefaultAngleMultiplier;

    /// <summary>
    /// Flat spread added to min/max after <see cref="AngleMultiplier"/>, so well-handled guns still suffer.
    /// </summary>
    [DataField]
    public Angle FlatAngle = Angle.FromDegrees(DefaultFlatAngleDegrees);

    /// <summary>
    /// Caps the max spread from this penalty, so guns can't end up firing sideways or backwards.
    /// </summary>
    [DataField]
    public Angle MaxAngle = Angle.FromDegrees(DefaultMaxAngleDegrees);
}
