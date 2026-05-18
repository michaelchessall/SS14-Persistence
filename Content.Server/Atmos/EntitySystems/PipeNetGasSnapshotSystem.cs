using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Map.Events;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// Captures and restores PipeNet gas mixtures during map serialization by storing each net's gas
/// on a single pipe node owner.
/// This intentionally allows for manual pipe deletions in the map editor to potentially wipe the contents of a pipenet
///  to reduce save speed and storage size.
/// </summary>
public sealed partial class PipeNetGasSnapshotSystem : EntitySystem
{
    [Dependency] private NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeforeSerializationEvent>(OnBeforeSerialization);
        SubscribeLocalEvent<PipeNetGasSnapshotComponent, NodeGroupsRebuilt>(OnNodeGroupsRebuilt);
    }

    // This function ensures no PipeNetGasSnapshotComponent exists on the map being saved
    // Then it stores every PipeNet into a PipeNetGasSnapshotComponent inside one of the pipe nodes.
    private void OnBeforeSerialization(BeforeSerializationEvent ev)
    {
        // Ensure all existing PipeNetGasSnapshotComponent entities are gone, to avoid adding gas contents from the previous save
        var toRemove = new List<EntityUid>();
        var snapshotQuery = EntityQueryEnumerator<PipeNetGasSnapshotComponent, TransformComponent>();
        while (snapshotQuery.MoveNext(out var uid, out _, out var xform)) // Grab all
        {
            if (!ev.MapIds.Contains(xform.MapID))   // Check the entity is on the map we're saving
                continue;

            toRemove.Add(uid);
        }
        foreach (var uid in toRemove)
            RemComp<PipeNetGasSnapshotComponent>(uid);


        var gridQuery = EntityQueryEnumerator<GridAtmosphereComponent, TransformComponent>();
        while (gridQuery.MoveNext(out _, out var atmos, out var xform))
        {
            if (!ev.MapIds.Contains(xform.MapID))   // Map check
                continue;

            foreach (var pipeNet in atmos.PipeNets)
            {
                if (!TrySelectSnapshotNode(pipeNet, out var owner, out var nodeName)) // Try to find a suitable node to store pipenet into
                    continue;

                // We intentionally store the full net on a single node owner. If that owner is removed or
                // ends up on a different net between save and load, the gas may be lost or restored to the
                // wrong net. This is accepted edge behavior.
                // If you want to change that, start here.
                var snapshot = EnsureComp<PipeNetGasSnapshotComponent>(owner);
                snapshot.NodeAir[nodeName] = new GasMixture(pipeNet.Air); // Copy pipenet gas mixture into new PipeNetGasSnapshotComponent
            }
        }
    }

    // This function is called when event NodeGroupsRebuilt occurs on an entity with our PipeNetGasSnapshotComponent
    // We check all is fine, and then transfer the gas mix from PipeNetGasSnapshotComponent into the PipeNet here
    private void OnNodeGroupsRebuilt(EntityUid uid, PipeNetGasSnapshotComponent component, ref NodeGroupsRebuilt args)
    {
        if (!TryComp(uid, out NodeContainerComponent? nodeContainer))
            return;

        var toRemove = new List<string>();
        foreach (var (nodeName, snapshotAir) in component.NodeAir)
        {
            // Just in-case the component has been broken from a map edit somehow - Shouldn't be necessary
            if (!_nodeContainer.TryGetNode(nodeContainer, nodeName, out PipeNode? pipeNode))
            {
                // Snapshot owner no longer has this node.
                toRemove.Add(nodeName);
                continue;
            }

            // Node group rebuild can run before this node joins a PipeNet; skip until it does.
            // This risks leaving PipeNetGasSnapshotComponent on a device which never joins a pipenet, but that is cleaned up
            // in OnBeforeSerialization
            if (pipeNode.NodeGroup is not IPipeNet pipeNet)
                continue;

            // Use the saved mixture as-is, even if the current net volume differs.
            pipeNet.Air = new GasMixture(snapshotAir);
            toRemove.Add(nodeName);
        }

        foreach (var nodeName in toRemove)
            component.NodeAir.Remove(nodeName);

        if (component.NodeAir.Count == 0)
            RemComp<PipeNetGasSnapshotComponent>(uid);
    }

    private bool TrySelectSnapshotNode(IPipeNet pipeNet, out EntityUid owner, out string nodeName)
    {
        owner = default;
        nodeName = string.Empty;

        foreach (var node in pipeNet.Nodes)
        {
            if (node is not PipeNode pipeNode)
                continue;

            if (string.IsNullOrEmpty(pipeNode.Name))
                continue;

            owner = pipeNode.Owner;
            nodeName = pipeNode.Name;
            return true;
        }

        return false;
    }

}
