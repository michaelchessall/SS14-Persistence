using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class StructureLeakVisualsComponent : Component
{
    [AutoNetworkedField]
    public bool Leaking;
}

[Serializable, NetSerializable]
public enum StructureLeakVisuals
{
    DamageLeak
}
