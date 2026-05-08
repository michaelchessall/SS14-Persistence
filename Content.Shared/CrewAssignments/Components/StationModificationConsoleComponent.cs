using Content.Shared.CrewAssignments.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.CrewAssignments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCrewAssignmentSystem))]
public sealed partial class StationModificationConsoleComponent : Component
{
}
