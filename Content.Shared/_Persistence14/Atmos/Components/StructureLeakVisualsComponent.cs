using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class StructureLeakVisualsComponent : Component
{
    [AutoNetworkedField]
    public bool Leaking;

    /// <summary>
    /// ID of the <see cref="LocalizedDatasetPrototype"/> containing messages to display if the structure is leaking or not.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Messages;
}

[Serializable, NetSerializable]
public enum StructureLeakVisuals
{
    DamageLeak
}
