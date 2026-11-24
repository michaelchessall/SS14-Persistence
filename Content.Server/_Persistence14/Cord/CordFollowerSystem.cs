using Content.Shared._Persistence14.Cord;
using Content.Shared._Persistence14.PersistentIdentifier;
using Robust.Server.GameObjects;

namespace Content.Server._Persistence14.Cord;

public sealed partial class CordFollowerSystem : EntitySystem
{
    [Dependency] private PersistentIdentifierSystem _pid = default!;
    [Dependency] private TransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CordComponent, CordFollowerComponent>();

        while (query.MoveNext(out var uid, out var cord, out var follower))
        {
            if (_pid.TryResolveId(follower.StartEntityTarget, out var start))
                cord.StartPosition = _transform.GetMapCoordinates(start.Owner).Position + follower.StartOffset;

            if (_pid.TryResolveId(follower.EndEntityTarget, out var end))
                cord.EndPosition = _transform.GetMapCoordinates(end.Owner).Position + follower.EndOffset;

            Dirty(uid, cord);
        }
    }
}