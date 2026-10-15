using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.TurretController;
using Content.Shared.Turrets;
using Robust.Server.GameObjects;
using System.Linq;
using Content.Shared.Station;
using Content.Shared.CrewAssignments.Components;
using Content.Server.Station.Systems;
using Content.Server.CrewAssignments.Systems;
using Content.Shared.Station.Components;
using System.Diagnostics;
using Content.Shared.Cargo.Events;

namespace Content.Server.TurretController;

/// <inheritdoc/>
public sealed partial class DeployableTurretControllerSystem : SharedDeployableTurretControllerSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    /// Keys for the device network. See <see cref="DeviceNetworkConstants"/> for further examples.
    public const string CmdSetArmamemtState = "set_armament_state";
    public const string CmdSetAccessExemptions = "set_access_exemption";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeployableTurretControllerComponent, BoundUIOpenedEvent>(OnBUIOpened);
        SubscribeLocalEvent<DeployableTurretControllerComponent, DeviceListUpdateEvent>(OnDeviceListUpdate);
        SubscribeLocalEvent<DeployableTurretControllerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<DeployableTurretControllerComponent, ComponentStartup>(TurretControllerStartup);
        SubscribeLocalEvent<StationGridAddedEvent>(OnStationGridAdded);
        SubscribeLocalEvent<StationGridRemovedEvent>(OnStationGridRemoved);
        SubscribeLocalEvent<StationCreateAssignment>(OnAssignmentCreate);
        SubscribeLocalEvent<StationDeleteAssignment>(OnAssignmentDelete);
        SubscribeLocalEvent<StationRenameAssignment>(OnAssignmentRename);



    }
    private void TurretControllerStartup(Entity<DeployableTurretControllerComponent> ent, ref ComponentStartup args) => UpdateNewStationAcces(ent);
    private void OnStationGridAdded(StationGridAddedEvent args) => CheckStationClaimChange(args.GridId);
    private void OnStationGridRemoved(StationGridRemovedEvent args) => CheckStationClaimChange(args.GridId);
    private void OnAssignmentCreate(StationCreateAssignment args) => CheckStationAssignmentChange(args.StationId);
    private void OnAssignmentDelete(StationDeleteAssignment args) => CheckStationAssignmentChange(args.StationId);
    private void OnAssignmentRename(StationRenameAssignment args) => CheckStationAssignmentChange(args.StationId);

    private void UpdateNewStationAcces(Entity<DeployableTurretControllerComponent> ent)
    {
        ent.Comp.AccessAssignments.Clear();
        var stationUID = _station.GetOwningStation(ent).GetValueOrDefault();
        if (TryComp<CrewAssignmentsComponent>(stationUID, out var crewAssignments) && crewAssignments != null)
        {
            foreach (var crewAssignment in crewAssignments.CrewAssignments)
            {
                ent.Comp.AccessAssignments.Add(crewAssignment.Value.ID);
                ent.Comp.AssignmentIdtoString[crewAssignment.Value.ID] = crewAssignment.Value.Name;
            }
        }
        Dirty(ent);

    }
    private void OnBUIOpened(Entity<DeployableTurretControllerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateNewStationAcces(ent);
        UpdateUIState(ent);

    }

    private void OnDeviceListUpdate(Entity<DeployableTurretControllerComponent> ent, ref DeviceListUpdateEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork))
            return;

        // List of new added turrets
        var turretsToAdd = args.Devices.Except(args.OldDevices);

        // Request data from newly linked devices
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
        };

        foreach (var turretUid in turretsToAdd)
        {
            if (!HasComp<DeployableTurretComponent>(turretUid))
                continue;

            if (!TryComp<DeviceNetworkComponent>(turretUid, out var turretDeviceNetwork))
                continue;

            _deviceNetwork.QueuePacket(ent, turretDeviceNetwork.Address, payload, device: deviceNetwork);
        }

        // Remove newly unlinked devices
        var turretsToRemove = args.OldDevices.Except(args.Devices);
        var refreshUi = false;

        foreach (var turretUid in turretsToRemove)
        {
            if (!TryComp<DeviceNetworkComponent>(turretUid, out var turretDeviceNetwork))
                continue;

            if (ent.Comp.LinkedTurrets.Remove(turretDeviceNetwork.Address))
                refreshUi = true;
        }

        if (refreshUi)
            UpdateUIState(ent);
    }

    private void OnPacketReceived(Entity<DeployableTurretControllerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork) || deviceNetwork.ReceiveFrequency != args.Frequency)
            return;

        // If an update was received from a turret, connect to it and update the UI
        if (command == DeviceNetworkConstants.CmdUpdatedState &&
            args.Data.TryGetValue(command, out DeployableTurretState updatedState))
        {
            ent.Comp.LinkedTurrets[args.SenderAddress] = updatedState;
            UpdateUIState(ent);
        }
    }

    protected override void ChangeArmamentSetting(Entity<DeployableTurretControllerComponent> ent, int armamentState, EntityUid? user = null)
    {
        base.ChangeArmamentSetting(ent, armamentState, user);

        if (!TryComp<DeviceNetworkComponent>(ent, out var device))
            return;

        // Update linked turrets' armament statuses
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdSetArmamemtState,
            [CmdSetArmamemtState] = armamentState,
        };

        _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $"{ToPrettyString(user)} set {ToPrettyString(ent)} to {armamentState}");

        _deviceNetwork.QueuePacket(ent, null, payload, device: device);
    }

    protected override void ChangeExemptAccessAssignments(
        Entity<DeployableTurretControllerComponent> ent,
        HashSet<int> exemptions,
        bool enabled,
        EntityUid? user = null
    )
    {
        base.ChangeExemptAccessAssignments(ent, exemptions, enabled, user);

        if (!TryComp<DeviceNetworkComponent>(ent, out var device) ||
            !TryComp<TurretTargetSettingsComponent>(ent, out var turretTargetingSettings))
            return;

        // Update linked turrets' target selection exemptions
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdSetAccessExemptions,
            [CmdSetAccessExemptions] = turretTargetingSettings.ExemptAccessAssignments,
        };

        foreach (var exemption in exemptions)
        {
            _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $"{ToPrettyString(user)} set {ToPrettyString(ent)} authorization of {exemption} to {enabled}");
        }

        _deviceNetwork.QueuePacket(ent, null, payload, device: device);
    }

    protected override void ClearExemptAccessAssignments(Entity<DeployableTurretControllerComponent> ent)
    {
        base.ClearExemptAccessAssignments(ent);

        if (!TryComp<DeviceNetworkComponent>(ent, out var device) ||
            !TryComp<TurretTargetSettingsComponent>(ent, out var turretTargetingSettings))
            return;

         // Update linked turrets' cleared target selection exemptions
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdSetAccessExemptions,
            [CmdSetAccessExemptions] = turretTargetingSettings.ExemptAccessAssignments,
        };
        _deviceNetwork.QueuePacket(ent, null, payload, device: device);
    }

    private void UpdateUIState(Entity<DeployableTurretControllerComponent> ent)
    {
        var turretStates = new Dictionary<string, string>();

        foreach (var (address, state) in ent.Comp.LinkedTurrets)
        {
            var stateName = state.ToString().ToLower();
            var stateDesc = Loc.GetString("turret-controls-window-turret-" + stateName);
            turretStates.Add(address, stateDesc);
        }

        var uiState = new DeployableTurretControllerBoundInterfaceState(turretStates);
        _userInterfaceSystem.SetUiState(ent.Owner, DeployableTurretControllerUiKey.Key, uiState);
    }

    private void CheckStationClaimChange(EntityUid grid)
    {
        var queries = EntityQueryEnumerator<DeployableTurretControllerComponent>();
        while (queries.MoveNext(out var turretControllerUid, out var turretControllerComponent))
        {
            var turretControllerGrid = Transform(turretControllerUid).ParentUid;
            if (turretControllerGrid  == grid)
            {
                turretControllerComponent.AccessAssignments.Clear();
                var turretControllerEntity = new Entity<DeployableTurretControllerComponent>(turretControllerUid, turretControllerComponent);
                ClearExemptAccessAssignments(turretControllerEntity);
                UpdateNewStationAcces(turretControllerEntity);
            }
        }
    }

    private void CheckStationAssignmentChange(EntityUid station)
    {
        var queries = EntityQueryEnumerator<DeployableTurretControllerComponent>();
        while (queries.MoveNext(out var turretControllerUid, out var turretControllerComponent))
        {
            var turretControllerStationUID = _station.GetOwningStation(turretControllerUid);
            if (turretControllerStationUID == station)
            {
                turretControllerComponent.AccessAssignments.Clear();
                var turretControllerEntity = new Entity<DeployableTurretControllerComponent>(turretControllerUid, turretControllerComponent);
                ClearExemptAccessAssignments(turretControllerEntity);
                UpdateNewStationAcces(turretControllerEntity);
            }
        }
    }
}
