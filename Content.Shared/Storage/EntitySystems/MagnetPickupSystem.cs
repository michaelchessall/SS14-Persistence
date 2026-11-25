using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MagnetPickupComponent"/>
/// </summary>
public sealed partial class MagnetPickupSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Reused list of nearby pickup candidates so we can sort them deterministically without allocating every scan.
    /// </summary>
    private readonly List<EntityUid> _nearby = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
    }

    private void OnMagnetMapInit(EntityUid uid, MagnetPickupComponent component, MapInitEvent args)
    {
        // Always magnet on map init
        component.NextScan = _timing.CurTime;
    }

    [SubscribeLocalEvent]
    private void GetAltVerbs(Entity<MagnetPickupComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        args.Verbs.Add(new AlternativeVerb
        {
            Message = ent.Comp.Active ? "Toggle Magnet Off" : "Toggle Magnet On",

            Act = () => ToggleMagnet(ent)
        });
    }

    // It... toggles the magnet. Between on and off. Pretty simple.
    private void ToggleMagnet(Entity<MagnetPickupComponent> ent)
    {
        ent.Comp.Active = !ent.Comp.Active;
        _appearance.SetData(ent.Owner, MagnetPickupVisuals.Enabled, ent.Comp.Active);
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MagnetPickupComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!CanSiphon((uid, comp)))
                continue;

            comp.NextScan = _timing.CurTime + ScanDelay;
            Dirty(uid, comp);
            Siphon((uid, comp));
        }
    }

    /// <summary>
    /// Determines if the given magnet entity is able to do its magnetism.
    /// </summary>
    private bool CanSiphon(Entity<MagnetPickupComponent> ent)
    {
        if (!ent.Comp.Active ||                     // Magnet Pickup Inactive
            ent.Comp.NextScan > _timing.CurTime)    // Not time to scan
            return false;

        var xform = Transform(ent.Owner);
        var parentUid = xform.ParentUid;

        if (ent.Comp.RequireActiveHand && (!_hands.TryGetActiveItem(parentUid, out var activeItem) || activeItem != ent.Owner))
            return false; // Magnet Pickup not in active hand

        if (ent.Comp.SlotFlags is not null)
        {
            if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot))
                return false; // Magnet Pickup not in any slot

            if ((slot.SlotFlags & ent.Comp.SlotFlags) == 0x0)
                return false; // Magnet Pickup not in any of the correct slots.
        }

        if (!HasComp<StorageComponent>(ent.Owner) || !_storage.HasSpace(ent.Owner))
            return false; // No space in bag

        return true;
    }

    /// <summary>
    /// Finds all nearby compatible entities and attempts to insert them into the magnet entity.
    /// </summary>
    private void Siphon(Entity<MagnetPickupComponent> ent)
    {
        var xform = Transform(ent.Owner);
        var parentUid = xform.ParentUid;
        var storage = EnsureComp<StorageComponent>(ent.Owner);

        var finalCoords = xform.Coordinates;
        var moverCoords = _transform.GetMoverCoordinates(ent.Owner, xform);

        _nearby.Clear();
        _nearby.AddRange(_lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries));

        var playedSound = false; // We only want to play the pickup sound once

        foreach (var item in _nearby)
        {
            if (!_whitelistSystem.IsWhitelistPassOrNull(storage.Whitelist, item) ||                                     // Only pickup compatible items
                item == parentUid)                                                                                      // It would be a little awkward to pickup yourself
                continue;

            // TODO: Probably move this to storage somewhere when it gets cleaned up
            // TODO: This sucks but you need to fix a lot of stuff to make it better
            // the problem is that stack pickups delete the original entity, which is fine, but due to
            // game state handling we can't show a lerp animation for it.
            var itemXform = Transform(item);
            var itemMap = _transform.GetMapCoordinates(item, xform: itemXform);
            var itemCoords = _transform.ToCoordinates(moverCoords.EntityId, itemMap);

            if (!_storage.Insert(ent.Owner, item, out var stacked, user: parentUid, storageComp: storage, playSound: !playedSound))
                continue;

            // Play pickup animation for either the stack entity or the original entity.
            if (stacked != null)
                _storage.PlayPickupAnimation(stacked.Value, itemCoords, finalCoords, itemXform.LocalRotation, parentUid);
            else
                _storage.PlayPickupAnimation(item, itemCoords, finalCoords, itemXform.LocalRotation, parentUid);

            playedSound = true;
        }
    }
}

[NetSerializable, Serializable]
public enum MagnetPickupVisuals
{
    Enabled,
}