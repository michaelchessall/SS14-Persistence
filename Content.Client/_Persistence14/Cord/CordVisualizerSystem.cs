using System.Numerics;
using Content.Shared._Persistence14.Cord;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;

namespace Content.Client._Persistence14.Cord;

public sealed partial class CordVisualizerSystem : SharedCordSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SpriteTreeSystem _spriteTree = default!;
    [Dependency] private PvsOverrideSystem _pvs = default!;

    private const float MinScale = 0.005f;

    [SubscribeLocalEvent]
    private void OnInit(EntityUid uid, CordComponent cord, ref ComponentInit args)
        => _pvs.AddGlobalOverride(uid);

    [SubscribeLocalEvent]
    private void OnShutdown(EntityUid uid, CordComponent cord, ref ComponentShutdown args)
        => _pvs.RemoveGlobalOverride(uid);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CordComponent, SpriteComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var cord, out var sprite, out var xform))
        {
            UpdateCord(uid, cord, sprite, xform);
        }
    }

    /// <summary>
    /// Updates a cord, setting all layers of its sprite to create the visual between the two points.
    /// </summary>
    private void UpdateCord(EntityUid uid, CordComponent cord, SpriteComponent sprite, TransformComponent xform)
    {
        if (sprite.LayerData.Count == 0)
            return;

        var diff = cord.EndPosition - cord.StartPosition;
        var length = diff.Length();
        var src = sprite.LayerData[0];
        if (src.ActualRsi is not { } rsi || !rsi.TryGetState(src.State, out var state))
            return;

        var tileLength = src.PixelSize.Y / (float)EyeManager.PixelsPerMeter;
        var lengthScale = length / tileLength;

        if (length < tileLength * MinScale || cord.WidthScale < MinScale)
        {
            _sprite.SetVisible((uid, sprite), false);
            return;
        }

        _sprite.SetVisible((uid, sprite), true);

        var midpoint = (cord.StartPosition + cord.EndPosition) / 2f;
        var worldRotation = new Angle(MathF.Atan2(diff.Y, diff.X) - MathF.PI / 2f);
        var (entPos, entRot) = _transform.GetWorldPositionRotation(xform);

        var localOffset = (-entRot).RotateVec(midpoint - entPos);

        _sprite.SetOffset((uid, sprite), localOffset);
        _sprite.SetRotation((uid, sprite), worldRotation - entRot);
        if (cord.Mode == CordRenderMode.Stretch)
        {
            _sprite.SetScale((uid, sprite), new Vector2(cord.WidthScale, lengthScale));
            _spriteTree.QueueTreeUpdate(uid, sprite, xform);
            return;
        }

        _sprite.SetScale((uid, sprite), Vector2.One);
        var layerCount = MathF.Ceiling(length / tileLength);

        while (sprite.LayerData.Count > layerCount)
        {
            _sprite.RemoveLayer((uid, sprite), sprite.LayerData.Count - 1);
        }

        while (sprite.LayerData.Count < layerCount)
        {
            var layer = _sprite.AddBlankLayer((uid, sprite));
            _sprite.LayerSetAnimationTime(layer, src.AnimationTime);
            _sprite.LayerSetRsi(layer, rsi, src.State);
        }

        for (int i = 0; i < layerCount; i++)
        {
            var layer = sprite.LayerData[i];

            var dist = i * tileLength;
            var segmentLength = MathF.Min(tileLength, length - dist);

            // Layers are centered, so positions are at each segment midpoint.
            var offset = -length / 2f + dist + segmentLength / 2f;

            _sprite.LayerSetOffset(layer, new Vector2(0f, offset));
            if (segmentLength / tileLength < MinScale)
                _sprite.LayerSetVisible(layer, false);
            else
            {
                _sprite.LayerSetScale(layer, new Vector2(cord.WidthScale, segmentLength / tileLength));
                _sprite.LayerSetVisible(layer, true);
            }
        }
    }
}