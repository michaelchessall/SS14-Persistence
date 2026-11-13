using Content.Server.Atmos.EntitySystems;
using Content.Shared._Persistence14.Botany;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Robust.Shared.Prototypes; // Persistence

namespace Content.Server.Botany.Systems;

public sealed partial class PlantConsumeExudeGasSystem : SharedPlantConsumeExudeGasSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private PlantSystem _plant = default!; // Persistence
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantTraySystem _tray = default!; // Persistence
    [Dependency] private IPrototypeManager _prototypeManager = default!; // Persistence

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantConsumeExudeGasComponent> ent, ref PlantGrowEvent args)
    {
        if (!TryComp<PlantComponent>(ent.Owner, out var plant)
            || !TryComp<PlantHolderComponent>(ent.Owner, out var plantHolder))
            return;

        if (!_plant.TryGetTray(ent.Owner, out var tray)) // Persistence
            return;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, true, true) ?? GasMixture.SpaceGas;

        // Persistence: Plant respiration
        foreach (var (gas, amount) in environment)
        {
            if (amount <= 0)
                continue;
            var gasPrototype = _atmosphere.GetGas(gas);
            if (gasPrototype.Nutrient == null)
                continue;
            var gasAbsorbed = Math.Min(amount, gasPrototype.PlantAbsorptionRate);
            var gasPercentage = amount / environment.TotalMoles;
            var nutrientToAbsorb = gasPrototype.MaxNutrient * gasPercentage - tray.Comp.Nutrients.GetValueOrDefault(gasPrototype.Nutrient.Value);
            gasAbsorbed = Math.Min(gasAbsorbed, (nutrientToAbsorb / gasPrototype.NutrientAmount).Float());
            if (gasAbsorbed <= 0)
                return;
            environment.AdjustMoles(gas, -gasAbsorbed);
            _tray.AdjustNutrient(tray.AsNullable(), gasPrototype.NutrientAmount * gasAbsorbed, gasPrototype.Nutrient.Value);
        }

        /* Persistence: Remove gas consumption.
        // Consume Gasses.
        plantHolder.MissingGas = false;
        var missingGas = 0;
        if (ent.Comp.ConsumeGasses.Count > 0)
        {
            foreach (var (gas, amount) in ent.Comp.ConsumeGasses)
            {
                if (environment.GetMoles(gas) < amount)
                {
                    missingGas++;
                    continue;
                }

                environment.AdjustMoles(gas, -amount);
            }

            if (missingGas > 0)
            {
                _plantHolder.AdjustsHealth(ent.Owner, -missingGas);
                plantHolder.MissingGas = true;
            }
        }
        */
        // Exude Gasses.
        var exudeCount = ent.Comp.ExudeGasses.Count;
        if (exudeCount > 0)
        {
            foreach (var (gas, amount) in ent.Comp.ExudeGasses)
            {
                environment.AdjustMoles(gas,
                    MathF.Max(1f, MathF.Round(_prototypeManager.Index(amount).BaseAmount / exudeCount))); // Persistence: Remove potency  TODO: Make gasses use nutrient requirements.
            }
        }

        Dirty(ent);
    }
}
