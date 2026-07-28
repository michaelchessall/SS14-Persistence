using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Searches the NPC's own hands, worn inventory slots, and one level into any worn storage
/// (backpack, belt, pockets, etc.) for a gun with usable ammo, and writes it to the target key if
/// found - a follow-up EquipOperator can then pick it up. Only recurses one level into storage,
/// enough to cover the common "gun in the backpack" case without the unbounded-recursion
/// complexity the stock RangedCombatCompound's own
///
/// Fails outright rather than succeeding empty if nothing qualifies, so the planner falls through
/// to whatever comes after (e.g. melee) instead of trying to equip nothing.
/// </summary>
public sealed partial class FindCarriedGunOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    /// <summary>Minimum fraction of ammo capacity (0-1) a candidate gun needs to qualify.</summary>
    [DataField("minPercent")]
    public float MinPercent = 0.001f;

    /// <summary>Blackboard key the found gun's EntityUid gets written to.</summary>
    [DataField("targetKey")]
    public string TargetKey = "Target";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var inventory = _entManager.System<InventorySystem>();

        foreach (var item in inventory.GetHandOrInventoryEntities(owner))
        {
            if (TryQualify(item, out var gun))
                return (true, new Dictionary<string, object> { { TargetKey, gun } });

            if (_entManager.TryGetComponent<StorageComponent>(item, out var storage))
            {
                foreach (var stored in storage.StoredItems.Keys)
                {
                    if (TryQualify(stored, out var storedGun))
                        return (true, new Dictionary<string, object> { { TargetKey, storedGun } });
                }
            }
        }

        return (false, null);
    }

    private bool TryQualify(EntityUid candidate, out EntityUid gun)
    {
        gun = candidate;

        if (!_entManager.HasComponent<GunComponent>(candidate))
            return false;

        var ammoEv = new GetAmmoCountEvent();
        _entManager.EventBus.RaiseLocalEvent(candidate, ref ammoEv);

        if (ammoEv.Capacity == 0)
            return false;

        var percent = (float)ammoEv.Count / ammoEv.Capacity;
        return percent >= MinPercent;
    }
}
