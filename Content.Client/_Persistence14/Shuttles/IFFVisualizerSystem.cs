using Content.Shared._Persistence14.Shuttles;
using Content.Shared.Shuttles.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Persistence14.Shuttles;

public sealed partial class IFFVisualizerSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<IFFUpdateEvent>(OnUpdateIff);
        SubscribeLocalEvent<IFFVisualizerComponent, MapInitEvent>(InitializeColor);
        SubscribeLocalEvent<IFFVisualizerComponent, ComponentStartup>(InitializeColor);
    }

    private void InitializeColor<TEvent>(EntityUid uid, IFFVisualizerComponent component, ref TEvent args)
    {
        if (!TryGetIffColor(uid, out var color, component.MaxIffSearchDepth))
            return;

        UpdateVisuals(uid, component, color);
    }

    private void OnUpdateIff(IFFUpdateEvent args)
    {
        if (args.Color is not { } color)
            return;

        var visualizers = EntityQueryEnumerator<IFFVisualizerComponent>();

        while (visualizers.MoveNext(out var uid, out var visualizer))
        {
            if (!TryMatchIff(uid, GetEntity(args.IffEnt), visualizer.MaxIffSearchDepth))
                continue;
            UpdateVisuals(uid, visualizer, color);
        }
    }

    private void UpdateVisuals(EntityUid uid, IFFVisualizerComponent component, Color color)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var layer in component.Layers)
        {
            _sprite.LayerSetColor((uid, sprite), layer, color);
        }
    }

    private bool TryMatchIff(EntityUid uid, EntityUid iffUid, int parentDepth)
    {
        if (uid == iffUid)
            return true;

        if (parentDepth <= 0)
            return false;

        return TryMatchIff(Transform(uid).ParentUid, iffUid, parentDepth - 1);
    }

    private bool TryGetIffColor(EntityUid uid, out Color color, int parentDepth)
    {
        if (TryComp<IFFComponent>(uid, out var iffComp))
        {
            color = iffComp.Color;
            return true;
        }

        if (parentDepth <= 0)
        {
            color = Color.White;
            return false;
        }

        return TryGetIffColor(Transform(uid).ParentUid, out color, parentDepth - 1);
    }
}