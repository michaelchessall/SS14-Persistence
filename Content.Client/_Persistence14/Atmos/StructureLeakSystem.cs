using Content.Client.IconSmoothing;
using Content.Shared._Persistence14.Atmos;
using Content.Shared._Persistence14.Atmos.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Persistence14.Atmos;

public sealed partial class StructureLeakSystem : EntitySystem
{
    [Dependency] private readonly IconSmoothSystem _smooth = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<WallLeakEvent>(OnStructureLeak);
        SubscribeLocalEvent<StructureLeakVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, StructureLeakVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (_appearance.TryGetData<int>(
                uid,
                StructureLeakVisuals.DamageLeak,
                out var leaking,
                args.Component) &&
            TryComp<IconSmoothComponent>(uid, out var iconSmooth))
        {
            _smooth.SetEnabled(uid, leaking == 0, iconSmooth);
        }
        if (args.Sprite == null)
            return;
        if (TryComp<SpriteComponent>(uid, out var sprite))
            MoveLeakLayerToTop(uid, sprite);

    }

    private void OnStructureLeak(WallLeakEvent args)
    {
        var uid = GetEntity(args.Entity);
        if (!Exists(uid) || !TryComp<IconSmoothComponent>(uid, out var iconSmooth))
            return;
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (args.Leaking)
            _appearance.SetData(uid, StructureLeakVisuals.DamageLeak, 1, appearance);
        else
            _appearance.SetData(uid, StructureLeakVisuals.DamageLeak, 0, appearance);

        _smooth.SetEnabled(uid, !args.Leaking, iconSmooth);
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
