using System.Diagnostics.CodeAnalysis;
using Content.Shared._Persistence14.Botany.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Persistence14.Botany.Systems;

public sealed partial class PlantRequirementsSystem : EntitySystem
{
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<PlantComponent> _plantQuery = default!;
    [Dependency] private EntityQuery<PlantBaseRequirementsComponent> _requirementsQuery = default!;
    [Dependency] private EntityQuery<PlantChemicalsComponent> _chemQuery = default!;

    [SubscribeLocalEvent(before: [typeof(PlantHarvestSystem)])]
    private void OnHarvestAttempt(Entity<PlantHolderComponent> ent, ref PlantHarvestAttemptEvent args)
    {
        if (!AllRequirementsFulfilled(ent))
        {
            _popup.PopupCursor(Loc.GetString("plant-component-low-nutrients-cant-harvest-message"), args.User);
            ent.Comp.ReadyForHarvest = false;
            Dirty(ent);
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Returns the total requirements for all nutrients in the plant.
    /// </summary>
    [PublicAPI]
    public Dictionary<ProtoId<PlantNutrientPrototype>, NutrientRequirement> GetAllRequirements(
        EntityUid ent)
    {
        var plantComp = _plantQuery.CompOrNull(ent);
        var baseComp = _requirementsQuery.CompOrNull(ent);
        var chemicalsComp = _chemQuery.CompOrNull(ent);

        return GetAllRequirements((ent, baseComp, chemicalsComp, plantComp));
    }

    [PublicAPI]
    public Dictionary<ProtoId<PlantNutrientPrototype>, NutrientRequirement> GetAllRequirements(
        Entity<PlantBaseRequirementsComponent?, PlantChemicalsComponent?, PlantComponent?> ent)
    {
        Dictionary<ProtoId<PlantNutrientPrototype>, FixedPoint2>? baseRequirements = null;
        Dictionary<ProtoId<ReagentPrototype>, PlantChemQuantity>? chemicals = null;
        var yield = 1;

        if (Resolve(ent, ref ent.Comp1, false))
            baseRequirements = ent.Comp1.BaseRequirements;

        if (Resolve(ent, ref ent.Comp2, false))
            chemicals = ent.Comp2.Chemicals;

        if (Resolve(ent, ref ent.Comp3, false))
            yield = ent.Comp3.Yield;

        return GetAllRequirements(baseRequirements, chemicals, yield);
    }

    [PublicAPI]
    public Dictionary<ProtoId<PlantNutrientPrototype>, NutrientRequirement> GetAllRequirements(
        Dictionary<ProtoId<PlantNutrientPrototype>, FixedPoint2>? baseRequirements,
        Dictionary<ProtoId<ReagentPrototype>, PlantChemQuantity>? chemicals,
        int yield)
    {
        Dictionary<ProtoId<PlantNutrientPrototype>, NutrientRequirement> totalRequirement = new();

        if (baseRequirements != null)
        {
            foreach (var (nutrient, baseRequirement) in baseRequirements)
            {
                NutrientRequirement requirement = new()
                {
                    Requirement = baseRequirement * yield,
                };
                totalRequirement.Add(nutrient, requirement);
            }
        }

        if (chemicals == null)
            return totalRequirement;

        foreach (var chemQuantity in chemicals.Values)
        {
            var quantityProto = _prototypeManager.Index(chemQuantity.Id);
            foreach (var (nutrient, requirement) in quantityProto.Requirements)
            {
                var newRequirement = totalRequirement.GetOrNew(nutrient);
                newRequirement.Requirement += requirement.Requirement * yield;
                newRequirement.BonusRequirement += requirement.BonusRequirement * yield;
                newRequirement.BonusAmount += requirement.BonusAmount * yield;
                totalRequirement[nutrient] = newRequirement;
            }
        }

        return totalRequirement;
    }

    /// <summary>
    /// Returns true if all the non-bonus requirements of the plant are fulfilled.
    /// </summary>
    [PublicAPI]
    public bool AllRequirementsFulfilled(EntityUid ent)
    {
        if (!_plant.TryGetTray(ent, out var tray))
            return false;

        return AllRequirementsFulfilled(ent, tray);
    }

    /// <summary>
    /// Returns true if all the non-bonus requirements of the plant are fulfilled by the nutrients in the tray.
    /// </summary>
    [PublicAPI]
    public bool AllRequirementsFulfilled(EntityUid ent, Entity<PlantTrayComponent> tray)
    {

        var requirements = GetAllRequirements(ent);
        var nutrients = tray.Comp.Nutrients;

        return AllRequirementsFulfilled(requirements, nutrients);
    }

    /// <summary>
    /// Returns true if all the non-bonus requirements are fulfilled by the nutrients.
    /// </summary>
    [PublicAPI]
    public bool AllRequirementsFulfilled(Dictionary<ProtoId<PlantNutrientPrototype>, NutrientRequirement> requirements,
        Dictionary<ProtoId<PlantNutrientPrototype>, FixedPoint2> nutrients)
    {
        foreach (var (nutrientId, requirement) in requirements)
        {
            if (requirement.Requirement == 0)
                continue;
            if (!nutrients.TryGetValue(nutrientId, out var nutrientAmount))
                return false;
            if (nutrientAmount < requirement.Requirement)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to return a whether each of the plant's requirements' mandatory and bonus requirements are fulfilled. Should only fail if a plant is not in a tray for some reason.
    /// </summary>
    [PublicAPI]
    public bool TryGetFulfilledRequirements(Entity<PlantComponent?> ent, [NotNullWhen(true)] out Dictionary<ProtoId<PlantNutrientPrototype>, (bool, bool)>? info)
    {
        if (!_plant.TryGetTray(ent, out var tray))
        {
            info = null;
            return false;
        }

        info = GetFulfilledRequirements(ent, tray);
        return true;
    }

    /// <summary>
    /// Returns a whether each of the plant's requirements' mandatory and bonus requirements are fulfilled by the tray's nutrients.
    /// </summary>
    [PublicAPI]
    public Dictionary<ProtoId<PlantNutrientPrototype>, (bool, bool)> GetFulfilledRequirements(EntityUid ent, Entity<PlantTrayComponent> tray)
    {
        var requirements = GetAllRequirements(ent);
        var nutrients = tray.Comp.Nutrients;

        return GetFulfilledRequirements(requirements, nutrients);
    }

    /// <summary>
    /// Returns a whether each of the requirements' mandatory and bonus requirements are fulfilled.
    /// </summary>
    [PublicAPI]
    public Dictionary<ProtoId<PlantNutrientPrototype>, (bool, bool)> GetFulfilledRequirements(Dictionary<ProtoId<PlantNutrientPrototype>, NutrientRequirement> requirements,
        Dictionary<ProtoId<PlantNutrientPrototype>, FixedPoint2> nutrients)
    {
        Dictionary<ProtoId<PlantNutrientPrototype>, (bool, bool)> info = new();

        foreach (var (nutrientId, requirement) in requirements)
        {
            var totalBonus = requirement.Requirement + requirement.BonusRequirement;

            if (!nutrients.TryGetValue(nutrientId, out var nutrientAmount))
                info[nutrientId] = (requirement.Requirement == 0, totalBonus == 0);
            else
                info[nutrientId] = (requirement.Requirement <= nutrientAmount, totalBonus <= nutrientAmount);
        }

        return info;
    }

}
