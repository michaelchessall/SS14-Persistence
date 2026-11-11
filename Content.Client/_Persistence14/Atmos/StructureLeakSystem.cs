using Content.Client.IconSmoothing;
using Content.Shared._Persistence14.Atmos.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._Persistence14.Atmos;

public sealed partial class StructureLeakSystem : EntitySystem
{
    [Dependency] private readonly IconSmoothSystem _smooth = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StructureLeakVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StructureLeakVisualsComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<StructureLeakVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnStartup(EntityUid uid, StructureLeakVisualsComponent component, ComponentStartup args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnAfterAutoHandleState(EntityUid uid, StructureLeakVisualsComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, StructureLeakVisualsComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, StructureLeakVisuals.DamageLeak, component.Leaking ? 1 : 0, appearance);
    }

    private void OnAppearanceChange(EntityUid uid, StructureLeakVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<IconSmoothComponent>(uid, out var iconSmooth))
            _smooth.SetEnabled(uid, !component.Leaking, iconSmooth);

        if (args.Sprite == null)
            return;

        var sprite = (uid, args.Sprite);
        if (_sprite.LayerMapTryGet(sprite, "leaking", out var layerIndex, false))
            _sprite.LayerSetVisible(sprite, layerIndex, component.Leaking);


        MoveLeakLayerToTop(uid, args.Sprite);
    }

    private void MoveLeakLayerToTop(EntityUid uid, SpriteComponent sprite)
    {
        var entity = (uid, sprite);
        if (!_sprite.LayerMapTryGet(entity, "leaking", out var layerIndex, false) ||
            layerIndex == sprite.LayerData.Count - 1 ||
            !_sprite.RemoveLayer(entity, layerIndex, out var layer, false) ||
            layer == null)
            return;

        var newLayerIndex = _sprite.AddLayer(entity, layer);
        _sprite.LayerMapSet(entity, "leaking", newLayerIndex);
    }

}
