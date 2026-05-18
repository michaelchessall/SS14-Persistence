using Content.Server.Chat.Managers;
using Content.Server.Shuttles.Components;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Server.Persistence.Systems;

public sealed partial class PersistenceSystem : EntitySystem
{
    [Dependency] private MapLoaderSystem _mapLoaderSys = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ActorSystem _actor = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IRobustRandom _random = default!;

    public bool LoadGrid(
        ResPath path, MapId mapId, Vector2 offset, Angle rot,
        out string? errorMessage, out Entity<MapGridComponent>? grid,
        DeserializationOptions? opts = null, bool dumpSpecialEntities = false
    )
    {
        if (opts == null)
            opts = DeserializationOptions.Default;
        errorMessage = null;

        if (!_mapLoaderSys.TryLoadGrid(mapId, path, out grid, opts, offset, rot))
        {
            errorMessage = $"Could not load the grid! Check console perhaps...";
            return false;
        }
        var gridUid = grid.Value;
        _transform.SetWorldPositionRotation(gridUid, offset, rot);

        if (dumpSpecialEntities) // On grid load, colliding entites arent inside the parent yet. Need to requeue
            Timer.Spawn(10, () => DumpSpecialEntities(gridUid));
        return true;
    }

    public bool SaveGrid(EntityUid gridUid, ResPath filePath, out string? errorMessage, bool dumpSpecialEntities = true, bool deleteGrid = false)
    {
        errorMessage = null;

        // no saving default grid
        if (!Exists(gridUid))
        {
            errorMessage = "That entity does not exist.";
            return false;
        }

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            errorMessage = "That entity does not contain a MapGridComponent.";
            return false;
        }

        if (dumpSpecialEntities)
            DumpSpecialEntities((gridUid, mapGrid));

        var saveSuccess = _mapLoaderSys.TrySaveGrid(gridUid, filePath);

        if (deleteGrid)
            QueueDel(gridUid);
        return saveSuccess;
    }

    private void DumpSpecialEntities(Entity<MapGridComponent> gridUid)
    {
        var transformQuery = GetEntityQuery<TransformComponent>();

        var gridXform = transformQuery.GetComponent(gridUid);
        var fromMatrix = _transform.GetWorldMatrix(gridXform);
        var fromMapUid = gridXform.MapUid;
        var fromRotation = _transform.GetWorldRotation(gridXform);
        if (!fromMapUid.HasValue) return;

        var actorQuery = EntityQueryEnumerator<ActorComponent>();
        var blacklistQuery = EntityQueryEnumerator<ArrivalsBlacklistComponent>();

        var gridWorldAABB =
            _transform.GetWorldMatrix(gridXform)
            .TransformBox(gridUid.Comp.LocalAABB)
            .Enlarged(0.2f);

        var toDump = new List<Entity<TransformComponent>>();
        FindDumpEntities(transformQuery, actorQuery, blacklistQuery, gridUid, toDump);
        foreach (var (ent, transform) in toDump)
        {
            var rotation = transform.LocalRotation;

            // From the current child's position, teleport it to somewhere close but outside the grid
            var entityPos = Vector2.Transform(transform.LocalPosition, fromMatrix);
            var direction = entityPos - gridWorldAABB.Center;
            if (direction.EqualsApprox(Vector2.Zero))
                direction = _random.NextAngle().RotateVec(Vector2.One);
            var newEntityPos = gridWorldAABB.ClosestPoint(entityPos + direction.Normalized() * gridWorldAABB.MaxDimension);

            _transform.SetCoordinates(ent, new EntityCoordinates(fromMapUid.Value, newEntityPos));
            _transform.SetWorldRotation(ent, fromRotation + rotation);
            if (_actor.TryGetSession(ent, out var session))
                _chat.DispatchServerMessage(session!, "Some unknown force has saved you from possibly killing yourself.");
        }
    }

    private static void FindDumpEntities(
        EntityQuery<TransformComponent> xformQuery,
        EntityQueryEnumerator<ActorComponent> actorQuery,
        EntityQueryEnumerator<ArrivalsBlacklistComponent> blacklistQuery,
        EntityUid gridUid, List<Entity<TransformComponent>> toDump
    )
    {
        void CheckAndQueueDump(EntityUid uid)
        {
            if (xformQuery.TryComp(uid, out var xform) && xform.GridUid == gridUid)
                toDump.Add((uid, xform));
        }

        while (actorQuery.MoveNext(out var uid, out _))
            CheckAndQueueDump(uid);
        while (blacklistQuery.MoveNext(out var uid, out _))
            CheckAndQueueDump(uid);
    }
}
