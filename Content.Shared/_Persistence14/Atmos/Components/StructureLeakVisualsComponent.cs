using Robust.Shared.Serialization;
namespace Content.Shared._Persistence14.Atmos.Components;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class StructureLeakVisualsComponent : Component
{
    [AutoNetworkedField]
    public bool Leaking;
}
