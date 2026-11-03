using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server._Persistence14.Power.Nodes;

[DataDefinition]
public sealed partial class CableJunctionNode : Node
{
    [DataField(required: true)]
    public Axis Axis;

    public override IEnumerable<Node> GetReachableNodes(Entity<TransformComponent> xform, EntityQuery<NodeContainerComponent> nodeQuery, EntityQuery<TransformComponent> xformQuery, Entity<MapGridComponent>? grid, IEntityManager entMan)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var mapSystem = entMan.System<SharedMapSystem>();
        var gridIndex = mapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        foreach (var (dir, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, gridEnt, gridIndex, mapSystem))
        {
            if (!MatchesAxis(dir))
                continue;

            if (node is CableNode)
                yield return node;
        }
    }

    public bool MatchesAxis(Direction dir)
    {
        return Axis switch
        {
            Axis.NorthSouth => dir is Direction.North or Direction.South,
            Axis.EastWest => dir is Direction.East or Direction.West,

            _ => false
        };
    }
}

public enum Axis
{
    NorthSouth,
    EastWest
}