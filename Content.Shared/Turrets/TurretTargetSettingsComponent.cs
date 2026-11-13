using Robust.Shared.GameStates;

namespace Content.Shared.Turrets;

/// <summary>
/// Attached to entities to provide them with turret target selection data.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TurretTargetSettingsSystem))]
public sealed partial class TurretTargetSettingsComponent : Component
{
    /// <summary>
    /// Crew with one or more access assignments from these lists are exempt from being targeted by turrets.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<int> ExemptAccessAssignments = new();

    /// <summary>
    /// Crew with one or more personal access names from these lists are exempt from being targeted by turrets.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public HashSet<string> ExemptAccessNames = new();
}
