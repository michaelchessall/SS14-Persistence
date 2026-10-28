using Robust.Shared.Prototypes;
using Content.Shared._Persistence14.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Shared._Persistence14.EntityEffects.Botany;

/// <summary>
/// Entity effect that adjusts the nutrition of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustNutritionEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustNutrient>
{
    [Dependency] private PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustNutrient> args)
    {
        _plantTray.AdjustNutrient(entity.AsNullable(), args.Effect.Amount, args.Effect.Nutrient);
    }
}

/// <summary>
/// A type of <see cref="EntityEffectBase{T}"/> which modifies the nutrient of a Seed in a PlantHolder.
/// These are not modified by scale as botany has no concept of scale.
/// </summary>
/// <typeparam name="T">The effect inheriting this BaseEffect</typeparam>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustNutrient : EntityEffectBase<PlantAdjustNutrient>
{
    /// <summary>
    /// How much we're adjusting the given nutrient by.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Amount { get; private set; } = 1;

    /// <summary>
    /// The given nutrient
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PlantNutrientPrototype> Nutrient { get; set; }

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return prototype.Resolve(Nutrient, out PlantNutrientPrototype? proto)
            ? Loc.GetString("entity-effect-guidebook-plant-nutrient",
                ("nutrient", Loc.GetString(proto.LocalizedName)),
                ("amount", Amount.ToString()),
                ("color", proto.SubstanceColor),
                ("chance", Probability))
            : null;

    }
}
