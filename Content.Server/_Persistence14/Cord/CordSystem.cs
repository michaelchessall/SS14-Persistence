using System.Numerics;
using Content.Shared._Persistence14.Cord;
using Content.Shared._Persistence14.Log;
using Content.Shared._Persistence14.PersistentIdentifier;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Persistence14.Cord;

public sealed partial class CordSystem : SharedCordSystem
{
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private PersistentIdentifierSystem _pid = default!;
    [Dependency] private PvsOverrideSystem _pvs = default!;

    [SubscribeLocalEvent]
    private void OnCordInit(EntityUid uid, CordComponent cord, ref ComponentInit args)
        => _pvs.AddGlobalOverride(uid);

    [SubscribeLocalEvent]
    private void OnCordShutdown(EntityUid uid, CordComponent cord, ref ComponentShutdown args)
        => _pvs.RemoveGlobalOverride(uid);

    /// <summary>
    ///     Spawns a cord entity from a prototype at the start point, configured to with both start and end points specified.
    /// </summary>
    public bool TrySpawnCord(MapId map, Vector2 startPosition, Vector2 endPosition, EntProtoId<CordComponent> prototype, out Entity<CordComponent> cord)
    {
        cord = default!;
        var uid = SpawnAtPosition(prototype, _transform.ToCoordinates(new MapCoordinates(startPosition, map)));
        if (!TryComp<CordComponent>(uid, out var cordComp))
        {
            // This really shouldn't happen...
            LogManager.GetSawmill("cord-system").Error($"Cord Prototype missing Cord Component");
            QueueDel(uid);
            return false;
        }

        cordComp.StartPosition = startPosition;
        cordComp.EndPosition = endPosition;
        Dirty(uid, cordComp);
        cord = (uid, cordComp);
        return true;
    }

    /// <summary>
    ///     Spawns a cord entity from a prototype whose start and end points are determined by the position of provided entities.
    ///     May be configured to set up the cord to follow the entities.
    /// </summary>
    public bool TrySpawnCord(EntityUid startEnt, EntityUid endEnt, EntProtoId<CordComponent> prototype, out Entity<CordComponent> cord,
                          bool setFollower = true, bool ensureFollower = true)
    {
        cord = default!;
        var startPos = _transform.GetMapCoordinates(startEnt).Position;
        var endPos = _transform.GetMapCoordinates(endEnt).Position;

        if (Transform(startEnt).MapID != Transform(endEnt).MapID)
            return LogManager.GetSawmill("cord-system").ErrorFalse($"Unable to form cord between entities on different maps.");

        if (!TrySpawnCord(Transform(startEnt).MapID, startPos, endPos, prototype, out cord))
            return false;

        if (!setFollower)
            return true; // All done! Cord set to current coordinate positions.

        if (!TryComp<CordFollowerComponent>(cord.Owner, out var follower))
        {
            if (!ensureFollower)  // No follower to set to follow
                return LogManager.GetSawmill("cord-system").InfoTrue($"Cord Prototype missing CordFollowerComponent when setFollower is True.");

            follower = AddComp<CordFollowerComponent>(cord.Owner);
        }

        follower.StartEntityTarget = _pid.EnsureId(startEnt);
        follower.EndEntityTarget = _pid.EnsureId(endEnt);
        return true;
    }
}