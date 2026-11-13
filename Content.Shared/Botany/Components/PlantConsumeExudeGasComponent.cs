using Content.Shared._Persistence14.Botany;
using Content.Shared.Atmos;
using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for gas to consume/exude on plant growth.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true, raiseAfterAutoHandleState: true)]
[Access(typeof(SharedPlantConsumeExudeGasSystem))]
public sealed partial class PlantConsumeExudeGasComponent : Component
{
    /* Persistence: Remove gas consumption, to be replaced with plant respiration
    /// <summary>
    /// Dictionary of gases and their consumption rates per growth tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Gas, float> ConsumeGasses = new();
    */

    /// <summary>
    /// Dictionary of gases and their exude rates per growth tick. Persistence: Use PlantChemQuantity instead of a float.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Gas, ProtoId<PlantGasQuantityPrototype>> ExudeGasses = new();
}
