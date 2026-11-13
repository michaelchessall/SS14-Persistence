using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;

namespace Content.Server.Botany.Systems;

public sealed partial class PlantAtmosphericSystem : SharedPlantAtmosphericSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;

    [Dependency] private EntityQuery<PlantHolderComponent> _holderQuery;

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantAtmosphericComponent> ent, ref PlantGrowEvent args) // Persistence: Replaced uses of component variables with modified versions.
    {
        if (!_holderQuery.TryComp(ent.Owner, out var holder))
            return;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, true, true) ?? GasMixture.SpaceGas;
        if (environment.Temperature < ent.Comp.ModifiedLowHeatTolerance || environment.Temperature > ent.Comp.ModifiedHighHeatTolerance)
        {
            _plantHolder.AdjustsHealth((ent.Owner, holder), -ent.Comp.ModifiedHeatToleranceDamage);
            holder.ImproperHeat = true;
        }
        else
            holder.ImproperHeat = false;

        var pressure = environment.Pressure;
        if (pressure < ent.Comp.ModifiedLowPressureTolerance || pressure > ent.Comp.ModifiedHighPressureTolerance)
        {
            _plantHolder.AdjustsHealth((ent.Owner, holder), -ent.Comp.ModifiedPressureToleranceDamage);
            holder.ImproperPressure = true;
        }
        else
            holder.ImproperPressure = false;

        Dirty(ent);
    }
}
