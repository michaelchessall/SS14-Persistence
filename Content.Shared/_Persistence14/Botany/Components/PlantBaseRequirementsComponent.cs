using Content.Shared._Persistence14.Botany;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Botany.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true, raiseAfterAutoHandleState: true)]
public sealed partial class PlantBaseRequirementsComponent : Component
{
    /// <summary>
    /// Persistence: Base amount of nutrients required for and consumed at harvest.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<PlantNutrientPrototype>, FixedPoint2> BaseRequirements = new();
}
