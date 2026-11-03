using Content.Server._Persistence14.Power.Nodes;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.Wires;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class CableVisSystem : EntitySystem
    {
        [Dependency] private SharedAppearanceSystem _appearance = default!;
        [Dependency] private NodeContainerSystem _nodeContainer = default!;
        [Dependency] private SharedMapSystem _map = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CableVisComponent, NodeGroupsRebuilt>(UpdateAppearance);
            SubscribeLocalEvent<CableJunctionVisComponent, NodeGroupsRebuilt>(UpdateAppearance);
        }

        private void UpdateAppearance(EntityUid uid, CableVisComponent cableVis, ref NodeGroupsRebuilt args)
        {
            if (!_nodeContainer.TryGetNode(uid, cableVis.Node, out Node? node))
                return;

            var mask = WireVisDirFlags.None;
            GetNodeDirMask(uid, node, ref mask);

            _appearance.SetData(uid, WireVisVisuals.ConnectedMask, mask);
        }

        private void UpdateAppearance(EntityUid uid, CableJunctionVisComponent junctionVis, ref NodeGroupsRebuilt args)
        {
            var mask = WireVisDirFlags.None;

            foreach (var nodeName in junctionVis.Nodes)
            {
                if (!_nodeContainer.TryGetNode(uid, nodeName, out Node? node))
                    continue;

                GetNodeDirMask(uid, node, ref mask);
            }

            _appearance.SetData(uid, WireVisVisuals.ConnectedMask, mask);
        }

        private void GetNodeDirMask(EntityUid uid, Node node, ref WireVisDirFlags mask)
        {
            var transform = Transform(uid);
            if (!TryComp<MapGridComponent>(transform.GridUid, out var grid))
                return;
            var tile = _map.TileIndicesFor((transform.GridUid.Value, grid), transform.Coordinates);

            foreach (var reachable in node.ReachableNodes)
            {
                if (reachable is not CableNode && reachable is not CableJunctionNode)
                    continue;

                var otherTransform = Transform(reachable.Owner);
                var otherTile = _map.TileIndicesFor((transform.GridUid.Value, grid), otherTransform.Coordinates);
                var diff = otherTile - tile;

                var visDir = diff switch
                {
                    (0, 1) => WireVisDirFlags.North,
                    (0, -1) => WireVisDirFlags.South,
                    (1, 0) => WireVisDirFlags.East,
                    (-1, 0) => WireVisDirFlags.West,
                    _ => WireVisDirFlags.None
                };

                var dir = visDir switch
                {
                    WireVisDirFlags.North => Direction.North,
                    WireVisDirFlags.South => Direction.South,
                    WireVisDirFlags.East => Direction.East,
                    WireVisDirFlags.West => Direction.West,
                    _ => Direction.Invalid
                };

                if (reachable is CableJunctionNode junction && !junction.MatchesAxis(dir))
                    continue;

                mask |= visDir;
            }
        }
    }
}
