using Content.Shared.Tether;
using Robust.Shared.Timing;

namespace Content.Server.Tether;

/// <summary>
/// Server-side half of the tether visual effect
/// </summary>
public sealed class TetherVisualSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Spawns a new tether-visual entity from the given prototype, bridging source and target.
    /// The prototype should have a TetherVisualComponent (Source/Target get overwritten here
    /// regardless of whatever the prototype declares) plus a Sprite with at least SegmentCount
    /// layers for the client to individually position along the curve.
    /// </summary>
    /// <param name="connectDuration">
    /// If given, overrides the prototype's ConnectDuration - how long the tether visually takes
    /// to extend from source out to target ("reach out" travel time).
    /// </param>
    /// <param name="disconnectDuration">
    /// If given, overrides the prototype's DisconnectDuration - how long the tether visually
    /// takes to retract once BeginDisconnect is called, before it's deleted.
    /// </param>
    public EntityUid SpawnTether(EntityUid source, EntityUid target, string prototype,
        TimeSpan? connectDuration = null, TimeSpan? disconnectDuration = null)
    {
        var tether = Spawn(prototype, _transform.GetMapCoordinates(source));
        var comp = EnsureComp<TetherVisualComponent>(tether);
        comp.Source = source;
        comp.Target = target;
        comp.ConnectStartedAt = _timing.CurTime;

        if (connectDuration is { } connect)
            comp.ConnectDuration = connect;
        if (disconnectDuration is { } disconnect)
            comp.DisconnectDuration = disconnect;

        Dirty(tether, comp);

        Log.Info($"Spawned tether {ToPrettyString(tether)} from {ToPrettyString(source)} to {ToPrettyString(target)} " +
                 $"(connect: {comp.ConnectDuration.TotalSeconds:F2}s, disconnect: {comp.DisconnectDuration.TotalSeconds:F2}s).");

        return tether;
    }

    /// <summary>
    /// Starts the tether's retract animation; the entity deletes itself once DisconnectDuration
    /// has elapsed. Safe to call more than once (subsequent calls are ignored) and safe to call
    /// with a zero DisconnectDuration (deletes on the next update, matching the old instant
    /// behavior). Prefer this over deleting the tether entity directly, unless an instant
    /// disappearance is genuinely wanted.
    /// </summary>
    public void BeginDisconnect(EntityUid tetherUid, TetherVisualComponent? comp = null)
    {
        if (!Resolve(tetherUid, ref comp, false))
            return;

        if (comp.DisconnectStartedAt != null)
            return; // already disconnecting

        comp.DisconnectStartedAt = _timing.CurTime;
        Dirty(tetherUid, comp);

        Log.Info($"Tether {ToPrettyString(tetherUid)} disconnecting - retracting over {comp.DisconnectDuration.TotalSeconds:F2}s before deletion.");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TetherVisualComponent>();
        while (query.MoveNext(out var uid, out var tether))
        {
            if (!Exists(tether.Source) || !Exists(tether.Target))
            {
                // An endpoint is outright gone - no sane retract animation possible, just delete.
                QueueDel(uid);
                continue;
            }

            if (tether.DisconnectStartedAt is { } startedAt &&
                _timing.CurTime - startedAt >= tether.DisconnectDuration)
            {
                QueueDel(uid);
            }
        }
    }
}
