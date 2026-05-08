using Content.Shared.CrewAssignments.Prototypes;
using Content.Shared.Precursor;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CrewAssignments.Components;


[RegisterComponent, NetworkedComponent]
public sealed partial class PrecursorExtractorComponent : Component
{
    [DataField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(5);
}

