using Content.Shared.Botany.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Botany.Systems;

using Content.Shared.Botany.Components;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class PlantTolerancesSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private BotanySystem _botany = default!;

    [Dependency] private EntityQuery<PlantAtmosphericComponent> _atmosQuery = default!;
    [Dependency] private EntityQuery<PlantChemicalsComponent> _chemQuery = default!;
    [Dependency] private EntityQuery<PlantConsumeExudeGasComponent> _gasQuery = default!;

    [SubscribeLocalEvent]
    private void OnPlantAtmosphericInit(Entity<PlantAtmosphericComponent> ent, ref ComponentInit _)
    {
        UpdateModifiers(ent);
    }

    [PublicAPI]
    public bool TryUpdateModifiers(EntityUid? ent, EntProtoId? prototypeId)
    {
        if (!_botany.TryGetPlantComponent<PlantAtmosphericComponent>(ent, prototypeId, out var atmospheric))
        {
            return false;
        }
        _botany.TryGetPlantComponent<PlantChemicalsComponent>(ent, prototypeId, out var chemical);
        _botany.TryGetPlantComponent<PlantConsumeExudeGasComponent>(ent, prototypeId, out var gas);
        UpdateModifiers(atmospheric, chemical, gas);

        if (ent.HasValue && Resolve(ent.Value, ref atmospheric)) // I'm not fully sure if i actually have to do this.
            Dirty<PlantAtmosphericComponent>((ent.Value, atmospheric));

        return true;
    }

    [PublicAPI]
    public void UpdateModifiers(Entity<PlantAtmosphericComponent> ent)
    {
        var chemicals = _chemQuery.CompOrNull(ent);
        var gas = _gasQuery.CompOrNull(ent);
        UpdateModifiers(ent.Comp, chemicals, gas);
        Dirty(ent);
    }

    [PublicAPI]
    public void UpdateModifiers(PlantAtmosphericComponent comp, PlantChemicalsComponent? chemicals, PlantConsumeExudeGasComponent? gas)
    {
        comp.ModifiedHeatToleranceDamage = comp.HeatToleranceDamage;
        comp.ModifiedHighHeatTolerance = comp.HighHeatTolerance;
        comp.ModifiedLowHeatTolerance = comp.LowHeatTolerance;
        comp.ModifiedPressureToleranceDamage = comp.PressureToleranceDamage;
        comp.ModifiedHighPressureTolerance = comp.HighPressureTolerance;
        comp.ModifiedLowPressureTolerance = comp.LowPressureTolerance;

        if (chemicals != null)
        {
            foreach (var quantity in chemicals.Chemicals.Values)
            {
                var quantityProto = _prototypeManager.Index(quantity.Id);

                if (quantityProto.HeatToleranceModifier != null)
                {
                    var modifier = quantityProto.HeatToleranceModifier.Value;
                    comp.ModifiedHeatToleranceDamage *= modifier.DamageMultiplier;
                    comp.ModifiedHighHeatTolerance += modifier. HighToleranceModifier;
                    comp.ModifiedLowHeatTolerance += modifier. LowToleranceModifier;
                }

                if (quantityProto.PressureToleranceModifier != null)
                {
                    var modifier = quantityProto.PressureToleranceModifier.Value;
                    comp.ModifiedPressureToleranceDamage *= modifier.DamageMultiplier;
                    comp.ModifiedHighPressureTolerance += modifier. HighToleranceModifier;
                    comp.ModifiedLowPressureTolerance += modifier. LowToleranceModifier;
                }
            }
        }
    }
}
