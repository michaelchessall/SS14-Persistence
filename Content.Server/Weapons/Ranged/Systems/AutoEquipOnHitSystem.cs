using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Weapons.Ranged.Systems;

/// <summary>
///     Auto-equip items that have AutoEquipOnHitComponent when they hit people.
///     Removes AutoEquipOnHitComponent when the item equips successfully or lands.
/// </summary>
public sealed class AutoEquipOnHitSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoEquipOnHitComponent, ThrowDoHitEvent>(OnThrowDoHit);
        SubscribeLocalEvent<AutoEquipOnHitComponent, LandEvent>(OnLand);
    }

    private void OnThrowDoHit(Entity<AutoEquipOnHitComponent> ent, ref ThrowDoHitEvent args)
    {
        //Try to grab their InventoryComponent
        if (!_inventory.TryGetSlots(args.Target, out var slots))
            return;

        //Equip to the first free slot which this entity can be equipped to
        foreach (var slot in slots)
        {
            if (_inventory.TryGetSlotEntity(args.Target, slot.Name, out _))
                continue;

            if (_inventory.TryEquip(args.Target, args.Target, ent.Owner, slot.Name, silent: true))
            {
                RemCompDeferred<AutoEquipOnHitComponent>(ent.Owner);
                break;
            }
        }
    }

    private void OnLand(Entity<AutoEquipOnHitComponent> ent, ref LandEvent args)
    {
        RemCompDeferred<AutoEquipOnHitComponent>(ent.Owner);
    }
}
