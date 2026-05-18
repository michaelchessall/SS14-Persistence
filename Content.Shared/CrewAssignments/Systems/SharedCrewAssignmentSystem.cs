using Content.Shared.CrewAssignments.Components;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Robust.Shared.Serialization;
namespace Content.Shared.CrewAssignments.Systems;

public abstract partial class SharedCrewAssignmentSystem : EntitySystem
{
    [Dependency] private SharedStationSystem _station = default!;
    public override void Initialize()
    {
        base.Initialize();

    }
    public CrewAssignmentsComponent? GetCrewAssignmentsComponent(EntityUid stationId)
    {
        var target = _station.GetOwningStation(stationId);
        if (target == null) return null;

        if (!TryComp<CrewAssignmentsComponent>(target, out var crewComp))
        {
            return null;
        }

        return crewComp;
    }
    public void RemoveOwner(StationDataComponent comp, string owner)
    {
        comp.AddOwner(owner);
    }
}


[NetSerializable, Serializable]
public enum StationModUiKey : byte
{
    StationMod
}
