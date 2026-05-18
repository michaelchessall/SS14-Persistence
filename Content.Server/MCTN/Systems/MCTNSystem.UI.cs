using Content.Server.MCTN.Components;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Nodes;
using Content.Shared.Access.Systems;
using Content.Shared.Coordinates;
using Content.Shared.MCTN.BUIStates;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server.MCTN.Systems;

public sealed partial class MCTNSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private PopupSystem _popup = default!;

    private void InitializeUI()
    {
        Subs.BuiEvents<MCTNComponent>(MCTNConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpen);
            subs.Event<MCTNConnectMessage>(OnConnectRequest);
            subs.Event<MCTNDisconnectMessage>(OnDisconnectRequest);
            subs.Event<MCTNTogglePlugMessage>(OnTogglePortRequest);
        });
    }

    private void OnUiOpen(EntityUid uid, MCTNComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, MCTNComponent component)
    {
        if (_uiSystem.HasUi(uid, MCTNConsoleUiKey.Key))
        {
            if (!TryComp<NodeContainerComponent>(uid, out var container))
                return;
            var state = GetInterfaceState(new Entity<MCTNComponent, NodeContainerComponent>(uid, component, container));
            _uiSystem.SetUiState(uid, MCTNConsoleUiKey.Key, state);
        }
    }

    public MCTNBoundUserInterfaceState GetInterfaceState(Entity<MCTNComponent, NodeContainerComponent> entity)
    {
        // container.Nodes.ToDictionary(x => x.Key, x => x.Value.NodeGroup?.Nodes.Count > 1);
        var availableConnections = (entity.Comp1.Connection.GetValueOrDefault() != default ? [] : GetAvailableConnections(entity))
            .Select(x =>
            {
                _physx.TryGetDistance(entity, x, out float distance);
                return new MCTNAvailableConnection()
                {
                    Entity = GetNetEntity(x),
                    Name = Comp<MetaDataComponent>(x.Owner).EntityName,
                    Distance = distance,
                    Occupied = x.Comp.Connection.GetValueOrDefault() != default,
                    Position = x.Owner.ToCoordinates().Position,
                };
            });

        MCTNCurrentConnection? currentConnectionState = null;
        var currentConnectionEid = entity.Comp1.Connection.GetValueOrDefault();
        TryComp<MCTNConnectionComponent>(currentConnectionEid, out var connection);

        var counterpart = connection != null ? GetConnectionCounterpart(entity, connection) : default;
        TryComp<MCTNComponent>(counterpart, out var otherMctNode);

        if (connection != null)
        {

            _physx.TryGetDistance(entity, counterpart, out var distance);
            currentConnectionState = new()
            {
                Entity = GetNetEntity(counterpart),
                Name = Comp<MetaDataComponent>(counterpart).EntityName,
                Distance = distance,
                Occupied = true,
                Position = counterpart.ToCoordinates().Position,
            };
        }

        // Plug states
        var plugStates = new Dictionary<string, MCTNBasePlugState>();
        foreach (var plugEntry in GetPlugNodes(entity.Owner))
        {
            var counterpartNode = counterpart != default ? GetPlugNode(counterpart!, plugEntry.Key) : null;

            MCTNBasePlugState portData;
            if (plugEntry.Value is CableDeviceNode deviceNode)
            {
                static MCTNPowerState FromNet(PowerNet net) =>
                    new()
                    {
                        CombinedLoad = net.NetworkNode.LastCombinedLoad,
                        CombinedSupply = net.NetworkNode.LastCombinedSupply,
                        CombinedMaxSupply = net.NetworkNode.LastCombinedMaxSupply
                    };

                MCTNPowerState localState = new();
                if (deviceNode.NodeGroup is PowerNet net)
                    localState = FromNet(net);

                MCTNPowerState remoteState = new();
                if (counterpartNode != null && counterpartNode is CableDeviceNode && counterpartNode.NodeGroup is PowerNet remoteNet)
                    remoteState = FromNet(remoteNet);

                portData = new MCTNPowerPlugState(localState, remoteState);
            }
            else if (plugEntry.Value is PortPipeNode pipeNode)
            {
                static MCTNPipeState FromNet(PipeNet net) =>
                    new()
                    {
                        GasMix = net.Air,
                    };

                MCTNPipeState localState = new();
                if (pipeNode.NodeGroup is PipeNet net)
                    localState = FromNet(net);

                MCTNPipeState remoteState = new();
                if (counterpartNode != null && counterpartNode is PortPipeNode && counterpartNode.NodeGroup is PipeNet remoteNet)
                    remoteState = FromNet(remoteNet);

                portData = new MCTNPipePlugState(localState, remoteState);
            }
            else
                continue;

            portData.Identifier = plugEntry.Key;
            portData.Enabled = IsPlugEnabled(entity.Comp1, plugEntry.Key);
            portData.IsNetworked = HasNodeNetwork(plugEntry.Value);
            portData.IsRemoteEnabled = otherMctNode != null && IsPlugEnabled(otherMctNode, plugEntry.Key);
            plugStates.Add(plugEntry.Key, portData);
        }

        return new()
        {
            MaxRange = entity.Comp1.MaxRange,
            AvailableConnections = availableConnections.ToList(),
            CurrentConnection = currentConnectionState,
            PlugStates = plugStates,
        };
    }

    private void OnConnectRequest(EntityUid uid, MCTNComponent component, MCTNConnectMessage args)
    {
        if (component.Connection.HasValue) return;
        if (Validate(uid, args))
            Connect(uid, GetEntity(args.Target));
    }

    private void OnDisconnectRequest(EntityUid uid, MCTNComponent component, MCTNDisconnectMessage args)
    {
        if (!component.Connection.HasValue) return;
        if (Validate(uid, args))
            Disconnect((uid, component));
    }

    private void OnTogglePortRequest(EntityUid uid, MCTNComponent component, MCTNTogglePlugMessage args)
    {
        if (Validate(uid, args))
            TogglePlugState((uid, component), args.Identifier);
    }

    private bool Validate(EntityUid uid, BaseBoundUserInterfaceEvent args)
    {
        var result = _access.IsAllowed(args.Actor, uid);
        if (!result)
            _popup.PopupEntity(Loc.GetString("network-configurator-device-access-denied"), uid, args.Actor);
        return result;
    }
}
