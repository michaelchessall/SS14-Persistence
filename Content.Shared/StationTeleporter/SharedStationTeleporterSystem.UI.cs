using Content.Shared.Pinpointer;
using Content.Shared.StationTeleporter.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.StationTeleporter;

public abstract partial class SharedStationTeleporterSystem
{
    private void InitializeUI()
    {
        SubscribeLocalEvent<StationTeleporterConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<StationTeleporterConsoleComponent, StationTeleporterClickMessage>(OnUIPortalClicked);
        SubscribeLocalEvent<StationTeleporterConsoleComponent, EntInsertedIntoContainerMessage>(OnChipStorageChanged);
        SubscribeLocalEvent<StationTeleporterConsoleComponent, EntRemovedFromContainerMessage>(OnChipStorageChanged);
    }

    private void OnUIOpened(Entity<StationTeleporterConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnUIPortalClicked(Entity<StationTeleporterConsoleComponent> ent,
        ref StationTeleporterClickMessage args)
    {
        ToggleTeleporterLink(ent, ref args);
        UpdateUserInterface(ent);
    }

    private void OnChipStorageChanged(Entity<StationTeleporterConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.ChipStorageName)
            UpdateUserInterface(ent);
    }

    private void OnChipStorageChanged(Entity<StationTeleporterConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.ChipStorageName)
            UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<StationTeleporterConsoleComponent> ent)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, StationTeleporterConsoleUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(ent);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        //Send data
        var teleportersData = new List<StationTeleporterStatus>();
        var cachedTeleporters = new HashSet<EntityUid>(); //Prevent UI teleporters duplication

        if (_container.TryGetContainer(ent, ent.Comp.ChipStorageName, out var container))
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (TryComp<TeleporterChipComponent>(entity, out var chipComp))
                    AddTeleporterFromChip(ent, (entity, chipComp), teleportersData, cachedTeleporters);
                else if (TryComp<HandTeleporterComponent>(entity, out var handTeleporterComp))
                    AddPortalsFromHandTeleporter(ent, (entity, handTeleporterComp), teleportersData, cachedTeleporters);
            }
        }

        _uiSystem.SetUiState(ent.Owner,
            StationTeleporterConsoleUIKey.Key,
            new StationTeleporterState(
                teleportersData,
                GetNetEntity(ent.Comp.SelectedTeleporter),
                GetNetEntity(ent),
                GetNetCoordinates(Transform(ent).Coordinates)));
    }

    private void AddTeleporterFromChip(Entity<StationTeleporterConsoleComponent> console, Entity<TeleporterChipComponent> chip, List<StationTeleporterStatus> teleportersData, HashSet<EntityUid> cachedTeleporters)
    {
        //Teleporter chips get portal links
        if (!Pid.TryResolveId(chip.Comp.ConnectedTeleporter, out var connected) || Deleted(connected))
            return;

        var teleporterName = _labelQuery.TryComp(connected, out var label)
            ? label.CurrentLabel ?? Loc.GetString("teleporter-name-unknown")
            : Loc.GetString("teleporter-name-unknown");

        var portalPos = _transform.GetWorldPosition(connected);
        var consolePos = _transform.GetWorldPosition(console);
        var distSqr = (portalPos - consolePos).LengthSquared();

        var canConnect = IsInRange(console, connected.Owner, chip.Comp.MaxConnectDistance);
        var canLocate = IsInRange(console, connected.Owner, chip.Comp.MaxLocateDistance);

        AddTeleporterStatus(console, connected, teleporterName, teleportersData, cachedTeleporters, canConnect, canLocate, chip);
    }

    private void AddPortalsFromHandTeleporter(Entity<StationTeleporterConsoleComponent> console, Entity<HandTeleporterComponent> handTeleporter, List<StationTeleporterStatus> teleportersData, HashSet<EntityUid> cachedTeleporters)
    {
        //RD handheld teleporter portals
        if (handTeleporter.Comp.FirstPortal is not null && Exists(handTeleporter.Comp.FirstPortal))
            AddTeleporterStatus(console, handTeleporter.Comp.FirstPortal.Value, Loc.GetString("teleporter-name-rd-first"), teleportersData, cachedTeleporters);

        if (handTeleporter.Comp.SecondPortal is not null && Exists(handTeleporter.Comp.SecondPortal))
            AddTeleporterStatus(console, handTeleporter.Comp.SecondPortal.Value, Loc.GetString("teleporter-name-rd-second"), teleportersData, cachedTeleporters);
    }

    /// <summary>
    /// Builds and appends a <see cref="StationTeleporterStatus"/> entry for the given teleporter, unless it was already added.
    /// </summary>
    private void AddTeleporterStatus(Entity<StationTeleporterConsoleComponent> console, EntityUid teleporter, string name, List<StationTeleporterStatus> teleportersData, HashSet<EntityUid> cachedTeleporters, bool connectRange = true, bool locateRange = true, Entity<TeleporterChipComponent>? chip = null)
    {
        if (!cachedTeleporters.Add(teleporter))
            return;

        Link.GetLink(teleporter, out var linkedTeleporter);
        EntityCoordinates? linkCoord = null;
        if (linkedTeleporter is not null && Pid.TryResolveId(linkedTeleporter.Value, out var linkedEnt))
            linkCoord = Transform(linkedEnt.Owner).Coordinates;

        teleportersData.Add(
            new StationTeleporterStatus(
                teleporterUid: GetNetEntity(teleporter),
                coordinates: GetNetCoordinates(Transform(teleporter).Coordinates),
                link: GetNetCoordinates(linkCoord),
                chip: GetNetEntity(chip),
                name: name,
                powered: Power.IsPowered(teleporter),
                canConnect: connectRange,
                canLocate: locateRange)
                );
    }
}
