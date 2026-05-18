using Content.Server.Station.Systems;
using Content.Shared.GridControl.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using System.Linq;

namespace Content.Server.GridControl.Systems;

[UsedImplicitly]
public sealed class GridSpawnerSystem : EntitySystem
{
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridSpawnerComponent, MapInitEvent>(OnSpawnerMapInit);
    }

    private void OnSpawnerMapInit(Entity<GridSpawnerComponent> ent, ref MapInitEvent args)
    {
        var entMap = _transform.GetMapId(ent.Owner);
        if (_loader.TryLoadGrid(entMap, ent.Comp.GridPath, out var grid, offset: _transform.GetMapCoordinates(ent.Owner).Position) && grid != null && ent.Comp.AddComponents != null)
        {
            EntityManager.AddComponents(grid.Value.Owner, ent.Comp.AddComponents);
            if (ent.Comp.StationGrid)
            {
                var station = _station.GetStations().FirstOrDefault(EntityUid.Invalid);
                if (station != EntityUid.Invalid)
                    _station.AddGridToStation(station, grid.Value);
            }
        }
    }
}
