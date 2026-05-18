using Content.Server.MCTN.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using System.Linq;

namespace Content.Server.MCTN.Systems;

public sealed partial class MCTNSystem : EntitySystem
{
    [Dependency] private NodeGroupSystem _nodeGroup = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;

    public Node? GetRemoteConnectionFor(EntityUid owner, MCTNComponent mctNode, Node thisNode)
    {
        // Node is disconnected
        if (!IsPlugEnabled(mctNode, thisNode.Name)) return null;

        var remoteConnectionEntity = mctNode.Connection.GetValueOrDefault();

        // MCTN does not have a connection established.
        if (remoteConnectionEntity == default) return null;
        if (!TryComp<MCTNConnectionComponent>(remoteConnectionEntity, out var connection)) return null;
        var remoteTarget = connection.AnchorA == owner ? connection.AnchorB : connection.AnchorA;
        if (remoteTarget == default) return null;

        // Remote Node is disconnected
        if (!TryComp<MCTNComponent>(remoteTarget, out var remoteMctNode)) return null;
        if (!IsPlugEnabled(remoteMctNode, thisNode.Name)) return null;

        // This really shouldnt happen but just in case..
        if (!TryComp<NodeContainerComponent>(owner, out var container)) return null;
        if (!_nodeContainer.TryGetNode<Node>(container, thisNode.Name, out var currentNode) || currentNode.NodeGroupID != thisNode.NodeGroupID) return null;

        if (!TryComp<NodeContainerComponent>(remoteTarget, out var remoteContainer)) return null;
        if (!_nodeContainer.TryGetNode<Node>(remoteContainer, thisNode.Name, out var remoteNode) || currentNode.NodeGroupID != remoteNode.NodeGroupID) return null;

        // Pipe layers
        if (thisNode is PipeNode thisPipe)
        {
            if (remoteNode is not PipeNode remotePipe) return null;
            if (thisPipe.CurrentPipeLayer != remotePipe.CurrentPipeLayer) return null;
        }

        return remoteNode;
    }

    public void TogglePlugState(Entity<MCTNComponent> ent, string plugIdentifier)
    {
        var newValue = !IsPlugEnabled(ent.Comp, plugIdentifier);
        SetPlugState(ent, plugIdentifier, newValue);
    }

    public void SetPlugState(Entity<MCTNComponent> ent, string plugIdentifier, bool newState)
    {
        var node = GetPlugNode(ent, plugIdentifier);
        if (node == null) return;

        ent.Comp.EnabledPlugs[plugIdentifier] = newState;

        _nodeGroup.QueueReflood(node);
        UpdateTethering(ent);

        UpdateUserInterface(ent.Owner, ent.Comp);
    }

    public Node? GetPlugNode(EntityUid uid, string plugIdentifier)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var container)) return null;
        if (!_nodeContainer.TryGetNode<Node>(container, plugIdentifier, out var node)) return null;
        return node;
    }

    public bool TryGetPlugNode(EntityUid uid, string plugIdentifier, out Node? node)
    {
        node = GetPlugNode(uid, plugIdentifier);
        return node != null;
    }

    public IEnumerable<KeyValuePair<string, Node>> GetPlugNodes(EntityUid uid)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var container)) return [];
        return GetPlugNodes(container);
    }

    public static IEnumerable<KeyValuePair<string, Node>> GetPlugNodes(NodeContainerComponent container)
    {
        return container.Nodes.AsEnumerable();
    }


    public static bool IsPlugEnabled(MCTNComponent mctNode, string plugIdentifier)
    {
        return mctNode.EnabledPlugs.GetValueOrDefault(plugIdentifier, false);
    }

    public bool IsPlugNetworked(Entity<NodeContainerComponent> entity, string plugIdentifier)
    {
        var node = GetPlugNode(entity, plugIdentifier);
        return node != null && HasNodeNetwork(node);
    }
    public static bool HasNodeNetwork(Node node)
    {
        return node.NodeGroup?.Nodes.Count > 1;
    }
}
