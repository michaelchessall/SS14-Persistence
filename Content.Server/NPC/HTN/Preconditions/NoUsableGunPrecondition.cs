using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Logical inverse of <see cref="GunAmmoPrecondition"/>, except a dry-but-reloadable gun (see
/// GunReloadUtility) never counts as "no usable gun" - without that carve-out, a find/equip-a-gun
/// branch gating on this would treat "dry but reloadable" the same as "no gun at all" and
/// endlessly try to replace a perfectly fine, still-reloadable, currently wielded gun.
/// </summary>
public sealed partial class NoUsableGunPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField]
    public float MinPercent = 0.001f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var gunSystem = _entManager.System<GunSystem>();

        if (!gunSystem.TryGetGun(owner, out var gun))
            return true;

        var ammoEv = new GetAmmoCountEvent();
        _entManager.EventBus.RaiseLocalEvent(gun, ref ammoEv);

        var percent = ammoEv.Capacity == 0 ? 0f : ammoEv.Count / (float)ammoEv.Capacity;
        percent = System.Math.Clamp(percent, 0f, 1f);

        if (percent >= MinPercent)
            return false;

        return !GunReloadUtility.CanReload(gun.Owner, owner, _entManager);
    }
}
