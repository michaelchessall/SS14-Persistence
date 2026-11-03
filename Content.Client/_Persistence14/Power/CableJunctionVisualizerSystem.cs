using Content.Client.SubFloor;
using Content.Shared.Wires;
using Robust.Client.GameObjects;
using Content.Shared._Persistence14.Power;

namespace Content.Client._Persistence14.Power;

public sealed partial class CableJunctionVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CableJunctionVisualizerComponent, AppearanceChangeEvent>(OnAppearanceChange, after: new[] { typeof(SubFloorHideSystem) });
    }
    private void OnAppearanceChange(EntityUid uid, CableJunctionVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.Visible)
            return;

        if (!_appearanceSystem.TryGetData<WireVisDirFlags>(uid, WireVisVisuals.ConnectedMask, out var mask, args.Component))
            mask = WireVisDirFlags.None;

        var north = (mask & WireVisDirFlags.North) > 0;
        var south = (mask & WireVisDirFlags.South) > 0;
        var east = (mask & WireVisDirFlags.East) > 0;
        var west = (mask & WireVisDirFlags.West) > 0;

        var horizontalState = (east, west) switch
        {
            (true, true) => CableJunctionVisualizerVariants.Horizontal,
            (true, false) => CableJunctionVisualizerVariants.East,
            (false, true) => CableJunctionVisualizerVariants.West,
            _ => CableJunctionVisualizerVariants.Center
        };

        var verticalState = (north, south) switch
        {
            (true, true) => CableJunctionVisualizerVariants.Vertical,
            (true, false) => CableJunctionVisualizerVariants.North,
            (false, true) => CableJunctionVisualizerVariants.South,
            _ => CableJunctionVisualizerVariants.None
        };

        _sprite.LayerSetRsiState((uid, args.Sprite), CableJunctionVisualLayers.Horizontal, component.GetRsiState(horizontalState));
        if (component.TryGetStripeRsiState(horizontalState, out var hStripeState))
            _sprite.LayerSetRsiState((uid, args.Sprite), CableJunctionVisualLayers.HorizontalStripe, hStripeState);
        if (verticalState == CableJunctionVisualizerVariants.None)
        {
            _sprite.LayerSetVisible((uid, args.Sprite), CableJunctionVisualLayers.Vertical, false);
            _sprite.LayerSetVisible((uid, args.Sprite), CableJunctionVisualLayers.VerticalStripe, false);
        }
        else
        {
            _sprite.LayerSetVisible((uid, args.Sprite), CableJunctionVisualLayers.Vertical, true);
            _sprite.LayerSetRsiState((uid, args.Sprite), CableJunctionVisualLayers.Vertical, component.GetRsiState(verticalState));
            if (component.TryGetStripeRsiState(verticalState, out var vStripeState))
                _sprite.LayerSetRsiState((uid, args.Sprite), CableJunctionVisualLayers.VerticalStripe, vStripeState);
        }
    }
}