using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Racks (closes the bolt on) the NPC's currently-held/innate gun if needed. Guns using
/// ChamberMagazineAmmoProviderComponent start with BoltClosed: false and can't fire at all until
/// racked, independent of ammo count (GunAmmoPrecondition doesn't check bolt state). Nothing in
/// the stock ranged-combat HTN chain does this, so this fills that gap. Always succeeds - this is
/// a "make sure it's ready" step, not a hard requirement.
///
/// Update()-only, not Plan()-based: Plan() can be speculatively backtracked out of (see
/// HTNPlanJob.RestoreTolastDecomposedTask), rolling back blackboard state but not real side
/// effects already performed (SetBoltClosed plays a sound and updates appearance), so this must
/// only run once a plan actually commits.
/// </summary>
public sealed partial class RackGunOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var gunSystem = _entManager.System<GunSystem>();

        if (gunSystem.TryGetGun(owner, out var gun) &&
            _entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(gun, out var chamber) &&
            chamber.BoltClosed == false)
        {
            gunSystem.SetBoltClosed(gun, chamber, true, owner);
        }

        return HTNOperatorStatus.Finished;
    }
}
