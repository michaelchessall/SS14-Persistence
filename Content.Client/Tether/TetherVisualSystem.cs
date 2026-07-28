using System.Numerics;
using Content.Shared.Tether;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Tether;

/// <summary>
/// Computes everything about how a tether looks, entirely client-side, every frame: the overall
/// position/rotation bridging Source and Target (deliberately NOT trusted from the server - see
/// TetherVisualComponent's doc comment for why that would reintroduce lag against a
/// client-predicted player), plus a curved "S-wiggle" shape built from multiple independently
/// positioned/rotated/scaled sprite layers rather than one rigid straight sprite. Both endpoints
/// of the curve stay exactly anchored to Source and Target - only the middle sways - achieved by
/// multiplying the animated sideways sway by an envelope that is mathematically zero at both
/// ends of the tether.
///
/// Also drives the connect ("reach out") and disconnect ("retract") travel animations: the
/// tether's visible tip extends from Source toward Target over ConnectDuration after appearing,
/// and retracts back into Source over DisconnectDuration once the server marks
/// DisconnectStartedAt. The base of the tether stays anchored to Source throughout both.
/// </summary>
public sealed class TetherVisualSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TetherVisualComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var tether, out var sprite))
        {
            if (!Exists(tether.Source) || !Exists(tether.Target))
                continue; // server will clean this up shortly, nothing to render meaningfully

            var sourceCoords = _transform.GetMapCoordinates(tether.Source);
            MapCoordinates targetCoords;

            if (tether.DisconnectStartedAt != null)
            {
                // Disconnecting - use a frozen snapshot of where the target was the moment
                // disconnection began, rather than continuing to track their live position.
                // Otherwise a victim who keeps moving after the tether breaks would drag the
                // retracting tether along with them, which reads as the tether never actually
                // letting go. Source has no equivalent freeze - it's a fixed anchor point
                // (the eye anomaly) that tethers always come and go from.
                tether.FrozenTargetCoords ??= _transform.GetMapCoordinates(tether.Target);
                targetCoords = tether.FrozenTargetCoords.Value;
            }
            else
            {
                targetCoords = _transform.GetMapCoordinates(tether.Target);
            }

            if (sourceCoords.MapId != targetCoords.MapId)
                continue;

            var delta = targetCoords.Position - sourceCoords.Position;
            var distance = delta.Length();

            if (distance <= 0.001f)
                continue; // avoid NaN direction when the two points briefly coincide exactly

            var direction = delta / distance;

            // Pull both ends in from the entities' exact centers, always along the direction
            // toward each other - lets an eye-shaped (or any non-circular) sprite have its
            // tether start near its edge instead of dead center, correctly no matter which
            // direction the other end happens to be in.
            if (tether.SourceOffsetDistance != 0f || tether.TargetOffsetDistance != 0f)
            {
                var adjustedSource = sourceCoords.Position + direction * tether.SourceOffsetDistance;
                var adjustedTarget = targetCoords.Position - direction * tether.TargetOffsetDistance;

                sourceCoords = new MapCoordinates(adjustedSource, sourceCoords.MapId);
                targetCoords = new MapCoordinates(adjustedTarget, targetCoords.MapId);

                delta = targetCoords.Position - sourceCoords.Position;
                distance = delta.Length();

                if (distance <= 0.001f)
                    continue; // the offsets ate up the whole distance between them - nothing sane to draw

                direction = delta / distance;
            }

            // Connect/disconnect travel animation: progress is the fraction of the full length
            // currently visible. The tip extends out from Source toward Target on connect, and
            // retracts back into Source on disconnect - the Source end stays anchored either way.
            var progress = 1f;

            if (tether.DisconnectStartedAt is { } disconnectStart)
            {
                progress = tether.DisconnectDuration <= TimeSpan.Zero
                    ? 0f
                    : 1f - (float)((_timing.CurTime - disconnectStart) / tether.DisconnectDuration);
            }
            else if (tether.ConnectStartedAt is { } connectStart && tether.ConnectDuration > TimeSpan.Zero)
            {
                progress = (float)((_timing.CurTime - connectStart) / tether.ConnectDuration);
            }

            progress = Math.Clamp(progress, 0f, 1f);

            var segments = Math.Max(1, tether.SegmentCount);
            var visibleDistance = distance * progress;

            // Fully retracted (or not yet extended at all) - hide every segment rather than
            // leaving the last-rendered frame frozen on screen while the server finishes
            // deleting the entity.
            if (visibleDistance <= 0.01f)
            {
                for (var i = 0; i < segments; i++)
                {
                    _sprite.LayerSetVisible((uid, sprite), i, false);
                }
                continue;
            }

            // The tether occupies only the first `progress` fraction of the source->target line,
            // so its own entity sits at the midpoint of that VISIBLE span, not the full span.
            var visibleMidpoint = sourceCoords.Position + direction * (visibleDistance / 2f);
            var angle = delta.ToWorldAngle();

            // Computed fresh from current (for the local player: predicted) positions every
            // single frame - deliberately not relying on this entity's own server-synced
            // Transform, which would lag behind by roughly one network round-trip.
            _transform.SetMapCoordinates(uid, new MapCoordinates(visibleMidpoint, sourceCoords.MapId));
            _transform.SetWorldRotation(uid, angle);

            // Seed off the entity's own ID so each tether's wiggle is stable and decorrelated
            // from every other tether, without needing to network anything for it.
            var seed = uid.GetHashCode() * 0.6180339887f;
            var timePhase = _timing.CurTime.TotalSeconds * tether.WiggleSpeed * MathHelper.TwoPi + seed;

            float Sway(float t)
            {
                if (!tether.Wiggle)
                    return 0f;

                var envelope = MathF.Sin(t * MathF.PI); // 0 at t=0 and t=1, peaks at t=0.5 - keeps both ends anchored
                var wave = MathF.Sin(t * tether.WiggleWaves * MathHelper.TwoPi + (float)timePhase);
                return tether.WiggleAmplitude * envelope * wave;
            }

            // Build the curve as a chain of N+1 points (in local space, Y = along the tether,
            // X = sideways sway) rather than computing each segment's center/tilt independently -
            // that independent approach rotates each segment around its own middle, which shifts
            // its endpoints away from wherever the neighboring segment's edge actually is. Here,
            // segment i is defined by exactly points[i] and points[i+1], so it always ends
            // precisely where the next segment begins, by construction rather than approximation.
            // The sway envelope's t runs over the VISIBLE length, so the moving tip is always an
            // anchored (sway-zero) point of the curve during connect/disconnect, exactly like the
            // fully-extended end state.
            var points = new Vector2[segments + 1];
            for (var j = 0; j <= segments; j++)
            {
                var t = (float)j / segments;
                var localY = (t - 0.5f) * visibleDistance;
                points[j] = new Vector2(Sway(t), localY);
            }

            for (var i = 0; i < segments; i++)
            {
                var start = points[i];
                var end = points[i + 1];
                var segDelta = end - start;
                var segCenter = (start + end) / 2f;
                var segLength = segDelta.Length();

                _sprite.LayerSetVisible((uid, sprite), i, true);
                _sprite.LayerSetOffset((uid, sprite), i, segCenter);
                _sprite.LayerSetRotation((uid, sprite), i, segDelta.ToWorldAngle());
                _sprite.LayerSetScale((uid, sprite), i, new Vector2(1f, segLength));
            }
        }
    }
}
