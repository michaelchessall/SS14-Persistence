using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;

namespace Content.Server.NPC.HTN;

/// <summary>
/// Shared "can this gun actually be reloaded right now" check, used by both ReloadGunOperator (to
/// decide whether to attempt a reload) and NoUsableGunPrecondition - so a dry-but-reloadable gun
/// is never treated the same as "no gun at all" by whatever HTN branch is asking.
/// </summary>
public static class GunReloadUtility
{
    public static bool CanReload(EntityUid gun, EntityUid owner, IEntityManager entManager)
    {
        var inventory = entManager.System<InventorySystem>();
        var whitelistSystem = entManager.System<EntityWhitelistSystem>();

        return CanItemSlotsReload(gun, owner, entManager, inventory, whitelistSystem) ||
               CanBallisticReload(gun, owner, entManager, inventory, whitelistSystem) ||
               CanRevolverReload(gun, owner, entManager, inventory, whitelistSystem);
    }

    private static bool CanItemSlotsReload(EntityUid gun, EntityUid owner, IEntityManager entManager,
        InventorySystem inventory, EntityWhitelistSystem whitelistSystem)
    {
        if (!entManager.TryGetComponent<ItemSlotsComponent>(gun, out var slots))
            return false;

        foreach (var (_, slot) in slots.Slots)
        {
            if (slot.Whitelist != null && FindOne(owner, slot.Whitelist, entManager, whitelistSystem, inventory) != null)
                return true;
        }

        return false;
    }

    private static bool CanBallisticReload(EntityUid gun, EntityUid owner, IEntityManager entManager,
        InventorySystem inventory, EntityWhitelistSystem whitelistSystem)
    {
        return entManager.TryGetComponent<BallisticAmmoProviderComponent>(gun, out var ballistic) &&
               ballistic.Whitelist != null &&
               FindOne(owner, ballistic.Whitelist, entManager, whitelistSystem, inventory) != null;
    }

    private static bool CanRevolverReload(EntityUid gun, EntityUid owner, IEntityManager entManager,
        InventorySystem inventory, EntityWhitelistSystem whitelistSystem)
    {
        return entManager.TryGetComponent<RevolverAmmoProviderComponent>(gun, out var revolver) &&
               revolver.Whitelist != null &&
               FindOne(owner, revolver.Whitelist, entManager, whitelistSystem, inventory) != null;
    }

    private static EntityUid? FindOne(EntityUid owner, EntityWhitelist whitelist, IEntityManager entManager,
        EntityWhitelistSystem whitelistSystem, InventorySystem inventory)
    {
        foreach (var item in inventory.GetHandOrInventoryEntities(owner))
        {
            if (TryQualify(item, whitelist, entManager, whitelistSystem))
                return item;

            if (entManager.TryGetComponent<StorageComponent>(item, out var storage))
            {
                foreach (var stored in storage.StoredItems.Keys)
                {
                    if (TryQualify(stored, whitelist, entManager, whitelistSystem))
                        return stored;
                }
            }
        }

        return null;
    }

    private static bool TryQualify(EntityUid candidate, EntityWhitelist whitelist, IEntityManager entManager,
        EntityWhitelistSystem whitelistSystem)
    {
        if (!whitelistSystem.IsValid(whitelist, candidate))
            return false;

        if (entManager.HasComponent<SpeedLoaderComponent>(candidate))
            return true;

        var ammoEv = new GetAmmoCountEvent();
        entManager.EventBus.RaiseLocalEvent(candidate, ref ammoEv);
        return ammoEv.Count > 0;
    }
}
