using Content.Shared._Persistence14.Botany;
using Content.Shared._Persistence14.Botany.Systems;
using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for atmospheric-related requirements for proper plant growth.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true, raiseAfterAutoHandleState: true)]
[Access(typeof(SharedPlantAtmosphericSystem), typeof(PlantTolerancesSystem))] // Persistence: Tolerance system access.
public sealed partial class PlantAtmosphericComponent : Component
{
    /// <summary>
    /// Damage per unit of heat tolerance exceeded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatToleranceDamage = 2f;

    /// <summary>
    /// Minimum temperature tolerance for plant growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LowHeatTolerance = 283f; // 10°C

    /// <summary>
    /// Maximum temperature tolerance for plant growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighHeatTolerance = 303f; // 30°C

    /// <summary>
    /// Damage per unit of pressure tolerance exceeded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PressureToleranceDamage = 2f;

    /// <summary>
    /// Minimum pressure tolerance for plant growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LowPressureTolerance = 81f; // 101 kPa

    /// <summary>
    /// Maximum pressure tolerance for plant growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighPressureTolerance = 121f; // 141 kPa

    /// <summary>
    /// Persistence: Damage per unit of heat tolerance exceeded, after applying modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifiedHeatToleranceDamage;

    /// <summary>
    /// Persistence: Minimum temperature tolerance for plant growth, after applying modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifiedLowHeatTolerance;

    /// <summary>
    /// Persistence: Maximum temperature tolerance for plant growth, after applying modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifiedHighHeatTolerance;

    /// <summary>
    /// Persistence: Damage per unit of pressure tolerance exceeded, after applying modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifiedPressureToleranceDamage;

    /// <summary>
    /// Persistence: Minimum pressure tolerance for plant growth, after applying modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifiedLowPressureTolerance;

    /// <summary>
    /// Persistence: Maximum pressure tolerance for plant growth, after applying modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifiedHighPressureTolerance;
}
