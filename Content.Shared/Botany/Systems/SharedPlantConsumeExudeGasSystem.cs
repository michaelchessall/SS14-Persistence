using Content.Shared._Persistence14.Botany;
using Content.Shared._Persistence14.Botany.Systems;
using Content.Shared._Persistence14.Random;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Prototypes; // Persistence
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Consumes and emits configured gases around plants each growth tick, then merges
/// the adjusted gas mixture back into the environment.
/// </summary>
public abstract partial class SharedPlantConsumeExudeGasSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;
    [Dependency] private PlantTolerancesSystem _tolerances = default!; // Persistence
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantConsumeExudeGasComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantConsumeExudeGasComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        // _mutation.CrossGasses(ent, ref ent.Comp.ConsumeGasses, pollenData.ConsumeGasses); Persistence: Remove ConsumeGasses
        _mutation.CrossGasses(ent, ref ent.Comp.ExudeGasses, pollenData.ExudeGasses);
        Dirty(ent);
    }

    /// <summary>
    /// Adds a random amount of a random gas to the exude gasses.
    /// </summary>
    [PublicAPI]
    public void MutateRandomExudeGasses(Entity<PlantConsumeExudeGasComponent?> ent, WeightedRandomFillPlantGasPrototype randomGasses) // Persistence: Use WeightedRandomFill
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var (gas, quantity) = randomGasses.Pick(random); // Persistence: Use RandomFill instead of just using a list of all gasses

        var gasses = ent.Comp.ExudeGasses;

        gasses[gas] = quantity;
        // if (!gasses.TryAdd(gas, amount)) // Persistence: Replace with above
        //     gasses[gas] += amount;

        _tolerances.TryUpdateModifiers(ent.Owner); // Persistence

        DirtyField(ent, nameof(ent.Comp.ExudeGasses));
    }

    /* Persistence: Remove gas consumption
    /// <summary>
    /// Adds a random amount of a random gas to the consume gasses.
    /// </summary>
    [PublicAPI]
    public void MutateRandomConsumeGasses(Entity<PlantConsumeExudeGasComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var gasses = ent.Comp.ConsumeGasses;
        var gas = random.Pick(Enum.GetValues<Gas>());

        if (!gasses.TryAdd(gas, amount))
            gasses[gas] += amount;

        DirtyField(ent, nameof(ent.Comp.ConsumeGasses));
    }*/
}
