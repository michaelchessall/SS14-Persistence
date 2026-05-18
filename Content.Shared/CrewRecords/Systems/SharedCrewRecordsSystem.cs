using Content.Shared.CrewRecords.Components;
using Content.Shared.Station;
namespace Content.Shared.CrewRecords.Systems;

public abstract partial class SharedCrewRecordSystem : EntitySystem
{
    [Dependency] private SharedStationSystem _station = default!;
    public override void Initialize()
    {
        base.Initialize();

    }

    public CrewRecordsComponent? GetCrewRecordsComponent(EntityUid stationId)
    {
        var target = _station.GetOwningStation(stationId);
        if (target == null) return null;

        if (!TryComp<CrewRecordsComponent>(target, out var crewComp))
        {
            return null;
        }

        return crewComp;
    }

}

