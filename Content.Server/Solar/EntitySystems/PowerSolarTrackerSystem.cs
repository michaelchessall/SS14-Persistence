using Content.Server.Solar.Components;
using JetBrains.Annotations;
using System.Linq;

namespace Content.Server.Solar.EntitySystems
{

    [UsedImplicitly]
    public sealed class PowerSolarTrackerSystem : EntitySystem
    {
        [Dependency] private SharedTransformSystem _transform = default!;

        public Dictionary<EntityUid, HashSet<EntityUid>> TrackersByGrid = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolarTrackerComponent, ComponentInit>(OnSolarTrackerInit);
            SubscribeLocalEvent<SolarTrackerComponent, ComponentShutdown>(OnSolarTrackerRemove);
            SubscribeLocalEvent<SolarTrackerComponent, GridUidChangedEvent>(OnSolarTrackerGridChanged);
        }


        private void OnSolarTrackerInit(Entity<SolarTrackerComponent> ent, ref ComponentInit args)
        {
            var gridUid = _transform.GetGrid(ent.Owner);
            if (!gridUid.HasValue) return;
            EnsureGridTracker(gridUid.Value);
            TrackersByGrid[gridUid.Value].Add(ent.Owner);
        }

        private void OnSolarTrackerRemove(Entity<SolarTrackerComponent> ent, ref ComponentShutdown args)
        {
            // Component Shutdown / Remove events happen when the entity no longer has a grid but do not trigger grid change
            // So this must be done to search and remove the entity from any hashmaps
            var set = TrackersByGrid.Values.FirstOrDefault(x => x.Contains(ent.Owner));
            set?.Remove(ent.Owner);
        }

        private void OnSolarTrackerGridChanged(Entity<SolarTrackerComponent> ent, ref GridUidChangedEvent args)
        {
            var oldGridTracker = args.OldGrid.HasValue ? TrackersByGrid.GetValueOrDefault(args.OldGrid.Value) : null;
            var newGridTracker = args.NewGrid.HasValue ? EnsureGridTracker(args.NewGrid.Value) : null;

            if (oldGridTracker != null)
                oldGridTracker.Remove(ent.Owner);
            if (newGridTracker != null)
                newGridTracker.Add(ent.Owner);
        }

        private HashSet<EntityUid> EnsureGridTracker(EntityUid gridUid)
        {
            if (!TrackersByGrid.ContainsKey(gridUid))
                TrackersByGrid[gridUid] = new();
            return TrackersByGrid[gridUid];
        }

        public EntityUid? GetGridTrackerEntity(EntityUid gridUid)
        {
            if (TrackersByGrid.TryGetValue(gridUid, out var value))
                return value.FirstOrDefault();
            return null;
        }
    }
}
