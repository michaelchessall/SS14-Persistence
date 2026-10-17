using JetBrains.Annotations;
using Content.Shared._Persistence14.Botany;
using Content.Shared._Persistence14.Botany.Components;
using Content.Shared._Persistence14.Random;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes; // Persistence
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles the chemicals of a plant.
/// </summary>
public sealed partial class PlantChemicalsSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantChemicalsComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantChemicalsComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossChemicals(ent, ref ent.Comp.Chemicals, pollenData.Chemicals);
        Dirty(ent);
    }

    /// <summary>
    /// Adds a random chemical to the plant chemicals. Persistence: Use WeightedRandomFillPlantChemicalPrototype
    /// </summary>
    [PublicAPI]
    public void MutateRandomChemical(Entity<PlantChemicalsComponent?> ent, WeightedRandomFillPlantChemicalPrototype randomChems)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var (chemicalId, quantity) = randomChems.Pick(random);
        //var amount = FixedPoint2.Max(random.NextFloat(0f, 1f) * quantity, FixedPoint2.Epsilon); Persistence: No more RNG here
        var seedChemQuantity = new PlantChemQuantity();

        seedChemQuantity.Id = quantity; // Persistence:
        seedChemQuantity.Inherent = false;

        /* Persistence: Replace this entire block of code with the two lines above
        if (ent.Comp.Chemicals.TryGetValue(chemicalId, out var value))
        {
            seedChemQuantity.Min = value.Min;
            seedChemQuantity.Max = value.Max + amount;
        }
        else
        {
            //Set the minimum to a fifth of the quantity to give some level of bad luck protection
            seedChemQuantity.Min = FixedPoint2.Clamp(quantity / 5f, FixedPoint2.Epsilon, 1f);
            seedChemQuantity.Max = seedChemQuantity.Min + amount;
            seedChemQuantity.Inherent = false;
        }
        */

        //var potencyDivisor = 100f / seedChemQuantity.Max;
        //seedChemQuantity.PotencyDivisor = (float)potencyDivisor;
        ent.Comp.Chemicals[chemicalId] = seedChemQuantity;
        Dirty(ent);
    }
}
