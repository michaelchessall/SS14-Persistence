using Content.Server.MiningFluid.Components;
using Content.Server.Worldgen.Components.Debris;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.MiningFluid;

public sealed class TrappedFluidSystem : EntitySystem
{
    [Dependency] private MapSystem _mapSys = default!;
    [Dependency] private IRobustRandom _random = default!;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<BlobFloorPlanBuilderComponent> _floorPlanBuilderQuery;

    private Queue<Entity<TrappedFluidComponent>> _pendingSeed = new();

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();
        _floorPlanBuilderQuery = GetEntityQuery<BlobFloorPlanBuilderComponent>();

        SubscribeLocalEvent<TrappedFluidComponent, ComponentStartup>(OnTrappedFluidStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingSeed.Count > 0)
        {
            HashSet<Entity<TrappedFluidComponent>> toRequeue = new();
            while (_pendingSeed.TryDequeue(out var entity))
            {
                var result = TrySeed(entity);
                if (result == TrySeedStatus.Retry)
                    toRequeue.Add(entity);
            }
            foreach (var ent in toRequeue)
                _pendingSeed.Enqueue(ent);
        }

    }

    private void OnTrappedFluidStartup(Entity<TrappedFluidComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Seeded) return;
        _pendingSeed.Enqueue(ent);
    }

    private TrySeedStatus TrySeed(Entity<TrappedFluidComponent> ent)
    {
        if (ent.Comp.Seeded) return TrySeedStatus.Complete;
        if (!_gridQuery.TryComp(ent, out var mapGrid) || mapGrid == null) return TrySeedStatus.DO_NOT_TRY;
        if (_floorPlanBuilderQuery.HasComp(ent)) return TrySeedStatus.Retry;

        var gridTiles = _mapSys.GetAllTiles(ent, mapGrid);
        var totalTiles = gridTiles.Count();

        var lowerBound = Math.Clamp(ent.Comp.SeedLowerBound, 0f, 1f);
        var upperBound = Math.Clamp(ent.Comp.SeedUpperBound, 0f, 1f);
        var multiplier = _random.NextFloat(lowerBound, upperBound);

        var seedMix = new GasMixture(ent.Comp.StaticMixture);
        foreach (var fluidEntry in ent.Comp.VariableMixture)
        {
            if (fluidEntry.Value.Moles <= 0 || fluidEntry.Value.Probability <= 0)
                continue;
            if (_random.Prob(fluidEntry.Value.Probability))
                seedMix.AdjustMoles(fluidEntry.Key, fluidEntry.Value.Moles);
        }
        ent.Comp.Air = new GasMixture(seedMix)
        {
            Volume = totalTiles * Atmospherics.CellVolume
        };
        ent.Comp.Air.Multiply(multiplier * totalTiles);

        ent.Comp.Seeded = true;
        return TrySeedStatus.Complete;
    }
}

public enum TrySeedStatus
{
    DO_NOT_TRY,
    Retry,
    Complete
}
