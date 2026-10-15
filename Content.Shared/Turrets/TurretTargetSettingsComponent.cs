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
    /// Crew with one or more access assignments or names from these lists are exempt from being targeted by turrets.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<int> ExemptAccessAssignments = new();

    // placeholder for now untill personal acces stuff is added
    [AutoNetworkedField]
    public HashSet<string> ExemptAccessNames = new();
}
