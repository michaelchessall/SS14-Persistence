using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Met only when the NPC's active hand (or itself, for an innate gun - see GunSystem.TryGetGun)
/// holds an actual gun that's both below MinPercent ammo AND cannot be reloaded (see
/// GunReloadUtility) - i.e. genuinely dead weight worth dropping outright.
///
/// Deliberately narrower than NoUsableGunPrecondition, which is also met with no gun at all - that
/// broader check is right for "go find/equip/pick up a gun", but wrong for "drop THIS gun", since
/// it would also fire while holding a perfectly good melee weapon with no gun in sight.
///
/// Meant to back its own standalone "drop it" branch rather than being bundled as prep ahead of a
/// "find a replacement" branch: HTNPlanJob only applies an operator's side effects once its entire
/// branch plans successfully, so bundling the drop ahead of a replacement search that can fail
/// (nothing else available) would roll the drop back on every replan, leaving a dead gun stuck in
/// hand.
/// </summary>
public sealed partial class HasUselessGunPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField]
    public float MinPercent = 0.001f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var gunSystem = _entManager.System<GunSystem>();

        if (!gunSystem.TryGetGun(owner, out var gun))
            return false;

        var ammoEv = new GetAmmoCountEvent();
        _entManager.EventBus.RaiseLocalEvent(gun, ref ammoEv);

        var percent = ammoEv.Capacity == 0 ? 0f : ammoEv.Count / (float)ammoEv.Capacity;
        percent = System.Math.Clamp(percent, 0f, 1f);

        if (percent >= MinPercent)
            return false;

        return !GunReloadUtility.CanReload(gun.Owner, owner, _entManager);
    }
}
