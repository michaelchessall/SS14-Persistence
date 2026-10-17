using JetBrains.Annotations;
using Content.Shared._Persistence14.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Prototypes; // Persistence 14

namespace Content.Shared.Botany.Systems;

public sealed partial class BotanySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!; // Persistence 14

    [SubscribeLocalEvent]
    private void OnProduceExamined(Entity<ProduceComponent> ent, ref ExaminedEvent args)
    {
        if (!TryGetPlantComponent<PlantComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var plant))
            return;

        using (args.PushGroup(nameof(ProduceComponent)))
        {
            foreach (var m in plant.Mutations)
            {
                // Don't show mutations that have no effect on produce (sentience)
                if (!m.AppliesToProduce)
                    continue;

                if (m.Description != null)
                    args.PushMarkup(Loc.GetString(m.Description));
            }
        }
    }

    private void ProduceGrown(Entity<ProduceComponent> ent, Dictionary<ProtoId<PlantNutrientPrototype>, FixedPoint2> nutrients, int yield) // Persistence: Plant nutrient rework
    {
        if (!TryGetPlantComponent<PlantComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var plant)
            || !TryGetPlantComponent<PlantChemicalsComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var chems))
            return;

        foreach (var mutation in plant.Mutations)
        {
            if (mutation.AppliesToProduce)
                _entityEffects.TryApplyEffect(ent.Owner, mutation.Effect);
        }

        _solutionContainer.EnsureSolution(ent.Owner, ent.Comp.TargetSolution, out var solution);
        solution.Comp.Solution.RemoveAllSolution();

        TryGetPlantRequirements(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var requirements); // Persistence

        foreach (var (chem, quantity) in chems.Chemicals) // Persistence: Heavily modified this loop for the plant nutrient rework.
        {
            var quantityProto = _prototypeManager.Index(quantity.Id);
            var amount = quantityProto.BaseAmount;
            foreach (var (nutrient, requirement) in quantityProto.Requirements)
            {
                var bonusNutrient = nutrients.GetValueOrDefault(nutrient) - requirements.GetValueOrDefault(nutrient).Requirement * yield;
                var bonusMultiplier = FixedPoint2.Zero;
                if (requirements[nutrient].BonusRequirement != FixedPoint2.Zero) // TryGetPlantRequirements gets all the requirements in PlantChemicalsComponent, so this should never cause issues.
                {
                    bonusMultiplier = FixedPoint2.Clamp(bonusNutrient / (requirements[nutrient].BonusRequirement * yield), FixedPoint2.Zero, 1);
                }
                amount += requirement.BonusAmount * bonusMultiplier;
            }
            solution.Comp.Solution.MaxVolume += amount;
            solution.Comp.Solution.AddReagent(chem, amount);
        }
    }

    /// <summary>
    /// Spawns a produce item from a plant and produces the produce.
    /// </summary>
    [PublicAPI]
    public void SpawnProduce(
        Entity<PlantComponent?, PlantDataComponent?> ent,
        EntityCoordinates position,
        int amount,
        Dictionary<ProtoId<PlantNutrientPrototype>, FixedPoint2> nutrients) // Persistence: Plant nutrient rework
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        for (var i = 0; i < amount; i++)
        {
            var product = random.Pick(ent.Comp2.ProductPrototypes);
            var entity = PredictedSpawnAtPosition(product, position);
            _randomHelper.RandomOffset(entity, 0.25f, random);

            var produce = EnsureComp<ProduceComponent>(entity);
            produce.PlantProtoId = MetaData(ent.Owner).EntityPrototype!.ID;
            produce.PlantData = ClonePlantSnapshotData(ent.Owner, parent: entity);
            Dirty(entity, produce);
            ProduceGrown((entity, produce), nutrients, amount); // Persistence: Plant nutrient rework
            //_appearance.SetData(entity, ProduceVisuals.Potency, ent.Comp1.Potency); // Persistence TODO: Make this not use potency
        }
    }
}
