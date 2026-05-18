using Content.Shared.Item;
using Content.Shared.Nutrition.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client.SprayPainter;

/// <summary>
/// Updates item sprites when they are repainted via the spray painter.
/// </summary>
public sealed partial class PaintableItemAppearanceSystem : VisualizerSystem<ItemComponent>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    protected override void OnAppearanceChange(EntityUid uid, ItemComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Prototype, out var protoName, args.Component))
            return;

        if (!_prototypeManager.HasIndex<EntityPrototype>(protoName))
            return;

        var temp = Spawn(protoName);

        if (TryComp<SpriteComponent>(temp, out var tempSprite))
        {
            var targetState = tempSprite.AllLayers.FirstOrDefault()?.RsiState ?? null;

            // Set layer state to a valid state in the target RSI before changing the base RSI
            if (args.Sprite.AllLayers.Any() && targetState != null)
            {
                SpriteSystem.LayerSetRsiState(uid, 0, targetState);
            }

            SpriteSystem.SetBaseRsi((uid, args.Sprite), tempSprite.BaseRSI);

            var hasOpenedState = AppearanceSystem.TryGetData<bool>(uid, OpenableVisuals.Opened, out var isOpened, args.Component);

            // Copy appearance data from the prototype variant (e.g., PDA type, accent colors)
            if (TryComp<AppearanceComponent>(temp, out _))
            {
                AppearanceSystem.CopyData(temp, (uid, args.Component));
            }

            // Preserve openable state
            if (hasOpenedState)
            {
                AppearanceSystem.SetData(uid, OpenableVisuals.Opened, isOpened, args.Component);
            }
        }

        QueueDel(temp);
    }
}
