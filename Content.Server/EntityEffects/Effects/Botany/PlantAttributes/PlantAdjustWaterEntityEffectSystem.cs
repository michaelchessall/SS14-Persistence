using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;
// TODO: Delete this entity effect after all usages have been replaced with PlantAdjustNutrient
public sealed partial class PlantAdjustWaterEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAdjustWater>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantAdjustWater> args)
    {
        _plantHolder.AdjustNutrient(entity, args.Effect.Amount, "water", entity);
        _plantHolder.AdjustNutrient(entity, args.Effect.Amount * -4, "toxin", entity);
    }
}
