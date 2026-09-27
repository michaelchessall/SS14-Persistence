using Content.Server.Worldgen.Components.Debris;
using Content.Server.Worldgen.Systems.Debris;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Server.Movement.Systems;

public sealed class MapBoundsSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformComponent, MoveEvent>(OnMovement);
        SubscribeLocalEvent<DebrisFeaturePlacerControllerComponent, PrePlaceDebrisFeatureEvent>(OnPrePlaceDebrisFeature);
    }

    private void OnPrePlaceDebrisFeature(Entity<DebrisFeaturePlacerControllerComponent> ent, ref PrePlaceDebrisFeatureEvent args)
    {
        var map = _xform.GetMap(args.Coords);
        if (!TryComp<MapBoundsComponent>(map, out var mapBounds) || mapBounds == null)
            return;

        var distSquared = Vector2.DistanceSquared(args.Coords.Position, Vector2.Zero);
        if (distSquared >= Math.Pow(mapBounds.Radius, 2))
            args.Handled = true;
    }

    private void OnMovement(Entity<TransformComponent> ent, ref MoveEvent args)
    {
        var map = _xform.GetMap(args.NewPosition);
        if (!TryComp<MapBoundsComponent>(map, out var mapBounds) || mapBounds == null)
            return;

        var distSquared = Vector2.DistanceSquared(args.NewPosition.Position, Vector2.Zero);
        if (distSquared < Math.Pow(mapBounds.Radius, 2))
            return;

        _physics.ApplyLinearImpulse(ent.Owner, (Vector2.Zero - args.NewPosition.Position).Normalized() * (MathF.Sqrt(distSquared) - mapBounds.Radius) * mapBounds.BaseImpulseVelocity);

    }
}
