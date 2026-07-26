using Content.Server.Atmos.EntitySystems;
using Content.Shared._Persistence14.Atmos.Geyser;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Persistence14.Atmos.Geyser;

public sealed partial class GasGeyserSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTime = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string Sawmill = "gas-geyser";

    public override void Update(float frameTime)
    {
        var geysers = EntityQueryEnumerator<GasGeyserComponent>();

        while (geysers.MoveNext(out var uid, out var geyserComp))
        {
            if (_gameTime.CurTime < geyserComp.NextEruptionTime)
                continue;

            if (!TryGetValidEnvironment((uid, geyserComp), out var environment))
                continue;

            _log.GetSawmill(Sawmill).Info("Geyser errupting");
            Errupt((uid, geyserComp), environment);
        }
    }

    /// <summary>
    /// Errupts a gas geyser, causing its gas mixture to be spewed into the environment.
    /// </summary>
    public void Errupt(Entity<GasGeyserComponent> geyser, GasMixture environment)
    {
        var moles = (float[]) geyser.Comp.Moles.Clone();

        foreach (var fluidEntry in geyser.Comp.VariableMoles)
        {
            if (fluidEntry.Value.Moles <= 0 || fluidEntry.Value.Probability <= 0)
                continue;

            if (_random.Prob(fluidEntry.Value.Probability))
                moles[(int) fluidEntry.Key] += fluidEntry.Value.Moles;
        }

        var merger = new GasMixture(moles, 1)
        {
            Temperature = geyser.Comp.SpawnTemperature,
        };
        _atmos.Merge(environment, merger);

        var delay = _random.NextFloat() * (geyser.Comp.MaxEruptionDelay - geyser.Comp.MinEruptionDelay) + geyser.Comp.MinEruptionDelay;
        geyser.Comp.NextEruptionTime = _gameTime.CurTime + delay;
        Dirty(geyser);

        RaiseNetworkEvent(new GasGeyserErruptedEvent(GetNetEntity(geyser.Owner)));
    }

    /// <summary>
    /// Determines if the environment is acceptable for the gase miner to errupt into, including pressure and qty restrictions on atmos.
    /// </summary>
    private bool TryGetValidEnvironment(Entity<GasGeyserComponent> geyserEnt, out GasMixture environment)
    {
        var (uid, geyser) = geyserEnt;
        var transform = Transform(uid);
        environment = default!;

        var mixture = _atmos.GetTileMixture((uid, transform), true);

        if (mixture is null)
            return false;

        if (mixture.TotalMoles >= geyser.MaxExternalMoles)
            return false;

        if (mixture.Pressure >= geyser.MaxExternalPressure)
            return false;

        environment = mixture!;
        return true;
    }
}