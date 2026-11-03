using Content.Shared._Persistence14.Random;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Persistence14.EntityEffects.Botany;

/// <summary>
/// Plant mutation entity effect that forces plant to exude gas while living.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateExudeGasesEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateExudeGases>
{
    [Dependency] private SharedPlantConsumeExudeGasSystem _plantConsumeExudeGas = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateExudeGases> args)
    {
        var randomGas = ProtoMan.Index(args.Effect.RandomPickBotanyGas);
        _plantConsumeExudeGas.MutateRandomExudeGasses(entity.Owner, randomGas);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateExudeGases : EntityEffectBase<PlantMutateExudeGases>
{
    /// <summary>
    /// The plant gas list this mutation draws from.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomFillPlantGasPrototype> RandomPickBotanyGas = "RandomPickGasMutagen";

    /// <inheritdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) // TODO: Update this at some point, Should probably be based off of PlantMutateChemicals.
    {
        return Loc.GetString("entity-effect-guidebook-plant-mutate-exude-gasses",
            ("chance", Probability));
    }
}
