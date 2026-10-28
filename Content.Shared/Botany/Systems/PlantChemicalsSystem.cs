using Content.Shared.Botany.Components;
using Content.Shared._Persistence14.Botany;
using Content.Shared._Persistence14.Botany.Components;
using Content.Shared._Persistence14.Botany.Systems;
using Content.Shared._Persistence14.Random;
using Content.Shared.Botany.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
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
    [Dependency] private PlantTolerancesSystem _tolerances = default!; // Persistence
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
    public void MutateRandomChemical(Entity<PlantChemicalsComponent?> ent, IReadOnlyList<ProtoId<WeightedRandomFillPlantChemicalPrototype>> randomChemTables)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var totalWeight = 0f;
        foreach (var table in randomChemTables)
        {
            foreach (var fill in ProtoMan.Index(table).Fills)
            {
                totalWeight += fill.Weight;
            }
        }

        if (totalWeight <= 0f)
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var selectedWeight = random.NextFloat() * totalWeight;
        var accumulatedWeight = 0f;
        WeightedRandomFillPlantChemicalPrototype? selectedTable = null;

        foreach (var table in randomChemTables)
        {
            var tableProto = ProtoMan.Index(table);
            foreach (var fill in tableProto.Fills)
            {
                accumulatedWeight += fill.Weight;
            }

            if (accumulatedWeight > selectedWeight)
            {
                selectedTable = tableProto;
                break;
            }
        }

        if (selectedTable == null)
            return;

        var (chemicalId, quantity) = selectedTable.Pick(random);

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

        _tolerances.TryUpdateModifiers(ent.Owner); // Persistence

        ent.Comp.Chemicals[chemicalId] = seedChemQuantity;
        Dirty(ent);
    }
}
