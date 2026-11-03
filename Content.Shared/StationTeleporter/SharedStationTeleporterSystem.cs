using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.PersistentIdentifier.Reference;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StationTeleporter.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.StationTeleporter;

/// <summary>
/// Handles <see cref="StationTeleporterComponent"/>, <see cref="StationTeleporterConsoleComponent"/> and
/// <see cref="TeleporterChipComponent"/> - linking teleporters together via chips recorded at a control console.
/// </summary>
public abstract partial class SharedStationTeleporterSystem : EntitySystem
{
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] protected SharedPowerReceiverSystem Power = default!;
    [Dependency] protected LinkedEntitySystem Link = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] protected PersistentIdentifierSystem Pid = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<LabelComponent> _labelQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeUI();

        SubscribeLocalEvent<StationTeleporterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationTeleporterComponent, LinkedEntityChangedEvent>(OnLinkedChanged);
        SubscribeLocalEvent<StationTeleporterComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<TeleporterChipComponent, ExaminedEvent>(OnChipExamined);
    }

    protected void RefreshAllConsoles()
    {
        var query = EntityQueryEnumerator<StationTeleporterConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            VerifyTeleporterState((uid, console));
            UpdateUserInterface((uid, console));
        }
    }

    private void ToggleTeleporterLink(Entity<StationTeleporterConsoleComponent> ent, ref StationTeleporterClickMessage args)
    {
        if (!TryGetEntity(args.Teleporter, out var teleporter))
            return;

        var chip = TryGetEntity(args.Chip, out var c) ? c : null;
        var maxDist = TryComp<TeleporterChipComponent>(chip, out var chipComp) ? chipComp.MaxConnectDistance : 0f;
        var distSqr = chip is null ? 0f :
            (_transform.GetWorldPosition(teleporter.Value) - _transform.GetWorldPosition(chip.Value)).LengthSquared();

        var canConnect = chip is null ? true : distSqr <= maxDist * maxDist;

        if (!Power.IsPowered(teleporter.Value) || !canConnect)
            return;

        // Not every linkable teleporter is a StationTeleporter (e.g. hand teleporter portals aren't),
        // so this component is optional here - only used for its LastLink/PortalColor bookkeeping.
        TryComp<StationTeleporterComponent>(teleporter.Value, out var stationTeleporterComponent);

        if (Link.GetLink(teleporter.Value, out var linkedTeleporter) && Pid.TryResolveId(linkedTeleporter.Value, out var linkedEnt))
        //If the pressed teleporter is linked to another - cut this connection.
        {
            Link.TryUnlink(teleporter.Value, linkedEnt.Owner);
            stationTeleporterComponent?.LastLink = PersistentIdentifierSystem.EmptyId;
        }
        else //If the pressed teleporter is not connected to anything...
        {
            if (ent.Comp.SelectedTeleporter is null) //And the console doesn't have teleporter selected - select it.
            {
                ent.Comp.SelectedTeleporter = teleporter;
            }
            else //If we have selected teleporter - tie them together
            {
                if (ent.Comp.SelectedTeleporter != teleporter.Value &&
                    Power.IsPowered(ent.Comp.SelectedTeleporter.Value))
                {
                    // Set the console's portal color on both sides before linking, so OnLinkedChanged
                    // can pick it up when it reacts to the link actually succeeding.
                    stationTeleporterComponent?.PortalColor = ent.Comp.PortalColor;

                    if (TryComp<StationTeleporterComponent>(ent.Comp.SelectedTeleporter.Value, out var selectedComponent))
                        selectedComponent.PortalColor = ent.Comp.PortalColor;

                    if (Link.TryLink(teleporter.Value, ent.Comp.SelectedTeleporter.Value) && stationTeleporterComponent is not null)
                        stationTeleporterComponent.LastLink = Pid.EnsureId(ent.Comp.SelectedTeleporter.Value);
                }

                ent.Comp.SelectedTeleporter = null;
            }
        }
    }

    private void OnInteractUsing(Entity<StationTeleporterComponent> teleporter, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<UseDelayComponent>(args.Used, out var useDelayComp) && _useDelay.IsDelayed((args.Used, useDelayComp)))
            return;

        _useDelay.TryResetDelay(args.Used);

        if (!TryComp<TeleporterChipComponent>(args.Used, out var chip))
            return;

        ConnectChipToTeleporter((args.Used, chip), teleporter);
        RefreshAllConsoles();

        _popup.PopupEntity(Loc.GetString("teleporter-console-chip-record"), teleporter, args.User);
        _audio.PlayPredicted(chip.RecordSound, args.Used, args.User);

        args.Handled = true;
    }

    protected void ConnectChipToTeleporter(Entity<TeleporterChipComponent> chip,
        Entity<StationTeleporterComponent> teleporter)
    {
        chip.Comp.ConnectedTeleporter = Pid.EnsureId(teleporter.Owner);

        chip.Comp.ConnectedName = _labelQuery.TryComp(teleporter, out var label)
            ? label.CurrentLabel ?? Loc.GetString("teleporter-name-unknown")
            : Loc.GetString("teleporter-name-unknown");

        Dirty(chip);
    }

    private void OnChipExamined(Entity<TeleporterChipComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.ConnectedTeleporter != PersistentIdentifierSystem.EmptyId
            ? Loc.GetString("teleporter-console-chip-examine-recorded", ("portal", ent.Comp.ConnectedName))
            : Loc.GetString("teleporter-console-chip-examine-null"));
    }

    private void OnLinkedChanged(Entity<StationTeleporterComponent> ent, ref LinkedEntityChangedEvent args)
    {
        var xform = Transform(ent);
        if (args.NewLinks.Count > 0)
        {
            _appearance.SetData(ent, TeleporterPortalVisuals.Color, ent.Comp.PortalColor);
            _ambient.SetAmbience(ent, true);
            _audio.PlayPvs(ent.Comp.LinkSound, xform.Coordinates);
        }
        else
        {
            _ambient.SetAmbience(ent, false);
            _audio.PlayPvs(ent.Comp.UnlinkSound, xform.Coordinates);
        }

        if (TryComp<LinkedEntityComponent>(ent.Owner, out var linked))
            foreach (var link in linked.LinkedEntities)
                if (!Pid.TryResolveId(link, out _))
                    Link.TrySever(ent.Owner, link);

        RefreshAllConsoles();
    }

    private void OnPowerChanged(Entity<StationTeleporterComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            if (Link.GetLink(ent, out var secondLink) && Pid.TryResolveId(secondLink.Value, out var linkedEnt))
                Link.TryUnlink(ent, linkedEnt.Owner);
        }
        else
            TryRestoreLink(ent);

        RefreshAllConsoles();
    }

    protected bool TryRestoreLink(Entity<StationTeleporterComponent> ent)
    {
        if (ent.Comp.LastLink == PersistentIdentifierSystem.EmptyId ||
            !Pid.TryResolveId<StationTeleporterComponent>(ent.Comp.LastLink, out var target) ||
            Power.IsPowered(ent.Owner) || Power.IsPowered(target.Owner) ||
            Link.GetLink(ent.Owner, out _) || Link.GetLink(target.Owner, out _))
            return false;

        Link.TryLink(ent, target);
        _appearance.SetData(ent, LinkedEntityVisuals.HasAnyLinks, false);
        _appearance.SetData(ent, LinkedEntityVisuals.HasAnyLinks, true);
        return true;
    }

    protected void RefreshVisuals(Entity<StationTeleporterComponent> ent)
    {
        _appearance.SetData(ent.Owner, LinkedEntityVisuals.HasAnyLinks, Link.GetLink(ent.Owner, out _));
        _appearance.SetData(ent, TeleporterPortalVisuals.Color, ent.Comp.PortalColor);
    }

    private bool IsInRange(EntityUid console, EntityUid portal, float range)
    {
        var consoleCoords = _transform.GetMapCoordinates(console);
        var targetCoords = _transform.GetMapCoordinates(portal);

        if (consoleCoords.MapId != targetCoords.MapId)
            return false;

        return (consoleCoords.Position - targetCoords.Position).LengthSquared() <= range * range;
    }

    private void VerifyTeleporterState(Entity<StationTeleporterConsoleComponent> console)
    {
        var teleporters = new List<(Entity<StationTeleporterComponent> teleporter, Entity<TeleporterChipComponent> chip)>();
        if (_container.TryGetContainer(console, console.Comp.ChipStorageName, out var container))
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (!TryComp<TeleporterChipComponent>(entity, out var chipComp) ||
                    !Pid.TryResolveId<StationTeleporterComponent>(chipComp.ConnectedTeleporter, out var connected) ||
                    !Link.GetLink(connected, out _)) // I don't care about non-connected portals.
                    continue;

                teleporters.Add((connected, (entity, chipComp)));
            }
        }

        var consoleCoords = _transform.GetWorldPosition(console.Owner);

        foreach (var (teleporter, chip) in teleporters)
        {
            var teleporterCoords = _transform.GetWorldPosition(teleporter);
            var sqrDist = (consoleCoords - teleporterCoords).LengthSquared();
            if (!Link.GetLink(teleporter.Owner, out var link) || !Pid.TryResolveId(link.Value, out var linkEnt) ||
                sqrDist <= chip.Comp.MaxConnectDistance * chip.Comp.MaxConnectDistance)
                continue;

            Link.TryUnlink(teleporter, linkEnt.Owner);
        }
    }
}
