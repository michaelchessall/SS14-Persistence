using System.Numerics;
using Content.Shared.Salvage.Magnet;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Salvage;

/// <summary>
/// Animated navigation beams drawn underneath grids and entity sprites.
/// </summary>
public sealed class SalvageMagnetTetherOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly SpriteSystem _sprites;
    private readonly ShaderInstance _shader;

    public SalvageMagnetTetherOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprites = _entities.System<SpriteSystem>();
        _shader = _prototypes.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        var query = _entities.EntityQueryEnumerator<SalvageMagnetTetherComponent>();

        while (query.MoveNext(out var tether))
        {
            if (tether.Start.MapId != args.MapId || tether.End.MapId != args.MapId)
                continue;

            var difference = tether.End.Position - tether.Start.Position;
            var length = difference.Length();
            if (length < 0.001f)
                continue;

            var texture = _sprites.GetFrame(tether.Sprite, _timing.CurTime);
            var width = texture.Width / (float) EyeManager.PixelsPerMeter;
            var tileLength = texture.Height / (float) EyeManager.PixelsPerMeter;
            var direction = difference / length;
            var angle = difference.ToWorldAngle();

            // Tile rather than stretch the animation over the whole journey.
            for (var distance = 0f; distance < length; distance += tileLength)
            {
                var segmentLength = MathF.Min(tileLength, length - distance);
                var center = tether.Start.Position + direction * (distance + segmentLength / 2f);
                var box = Box2.CenteredAround(center, new Vector2(width, segmentLength));
                var rotated = new Box2Rotated(box, angle, center);
                if (!rotated.CalcBoundingBox().Intersects(args.WorldAABB))
                    continue;

                handle.DrawTextureRect(texture, rotated, tether.Color);
            }
        }

        handle.UseShader(null);
    }
}
