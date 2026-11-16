using Content.Client._Persistence14.CharacterSnapshot;
using Content.Shared._Persistence14.CharacterSnapshot;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

// PS14 - "last seen" preview from the snapshot taken at the character's last save.
public sealed partial class ProfilePreviewSpriteView
{
    /// <summary>
    /// Builds the dummy from the character's last-saved snapshot, if there is one.
    /// </summary>
    /// <returns>False if there's no usable snapshot, in which case nothing was spawned.</returns>
    private bool TryLoadSnapshotEntity(HumanoidCharacterProfile humanoid, bool showClothes)
    {
        if (!EntMan.System<CharacterSnapshotSystem>().TryGetSnapshot(humanoid.Name, out var snapshot))
            return false;

        if (snapshot.Species is { } species && _prototypeManager.TryIndex(species, out var speciesProto))
        {
            var visualBody = EntMan.System<SharedVisualBodySystem>();
            PreviewDummy = EntMan.Spawn(speciesProto.DollPrototype, MapCoordinates.Nullspace);

            // Profile first for anything the snapshot doesn't cover, then the snapshot's actual look on top.
            if (species == humanoid.Species)
            {
                visualBody.ApplyProfileTo(PreviewDummy, humanoid);
                EntMan.System<HumanoidProfileSystem>().ApplyProfileTo(PreviewDummy, humanoid);
            }

            visualBody.ApplyProfiles(PreviewDummy, snapshot.OrganProfiles);
            visualBody.ApplyMarkings(PreviewDummy, snapshot.Markings);
        }
        else if (snapshot.Body is { } body && _prototypeManager.HasIndex<EntityPrototype>(body))
        {
            // Non-humanoid body, e.g. a borg chassis.
            PreviewDummy = EntMan.Spawn(body, MapCoordinates.Nullspace);
        }
        else
        {
            return false;
        }

        if (showClothes)
            GiveDummySnapshotGear(snapshot);

        return true;
    }

    private void GiveDummySnapshotGear(CharacterAppearanceSnapshot snapshot)
    {
        var inventorySys = EntMan.System<InventorySystem>();
        var handsSys = EntMan.System<SharedHandsSystem>();

        // Prototypes removed since the save, or slots/hands this dummy lacks, are skipped.
        foreach (var (slot, proto) in snapshot.Slots)
        {
            if (!_prototypeManager.HasIndex<EntityPrototype>(proto))
                continue;

            var item = EntMan.Spawn(proto, MapCoordinates.Nullspace);
            if (!inventorySys.TryEquip(PreviewDummy, item, slot, silent: true, force: true))
                EntMan.DeleteEntity(item);
        }

        foreach (var (hand, proto) in snapshot.Hands)
        {
            if (!_prototypeManager.HasIndex<EntityPrototype>(proto))
                continue;

            var item = EntMan.Spawn(proto, MapCoordinates.Nullspace);
            if (!handsSys.TryPickup(PreviewDummy, item, hand, checkActionBlocker: false, animate: false))
                EntMan.DeleteEntity(item);
        }
    }
}
