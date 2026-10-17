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
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!; // Persistence

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantConsumeExudeGasComponent> ent, ref PlantGrowEvent args)
    {
        if (!TryComp<PlantComponent>(ent.Owner, out var plant)
            || !TryComp<PlantHolderComponent>(ent.Owner, out var plantHolder))
            return;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, true, true) ?? GasMixture.SpaceGas;

        /* Persistence: Remove gas consumption. TODO: Allow plants to absorb nutrients from gasses.
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
