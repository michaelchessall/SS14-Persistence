using Content.Shared._Persistence14.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Persistence14.Weapons.Ranged.Components;

/// <summary>
/// Multiplies the gun's minimum and maximum spread angles while it is NOT wielded.
/// Requires a WieldableComponent on the same entity.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunPenaltySystem))]
public sealed partial class GunUnwieldedPenaltyComponent : Component
{
    /// <summary>
    /// Spread multiplier applied while unwielded. 1.3 means 30% worse accuracy.
    /// Scales min/max angle and recoil buildup, and divides recoil recovery.
    /// </summary>
    [DataField]
    public float AngleMultiplier = 1f;

    [DataField]
    public LocId? ExamineMessage = "gununwieldedpenalty-component-examine";
}
