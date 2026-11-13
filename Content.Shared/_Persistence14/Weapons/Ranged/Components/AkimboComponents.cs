using Content.Shared._Persistence14.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Persistence14.Weapons.Ranged.Components;

/// <summary>
/// Marks a gun as usable for akimbo firing. Holding two of these, one in each hand, grants the akimbo toggle action.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAkimboSystem))]
public sealed partial class AkimboCapableComponent : Component
{
    /// <summary>
    /// Flat spread added to the gun's minimum and maximum angle while akimbo is toggled on.
    /// Applied after multipliers like the unwielded penalty, so it isn't scaled by them.
    /// </summary>
    [DataField]
    public Angle SpreadPenalty = Angle.FromDegrees(5);
}

/// <summary>
/// Tracks the akimbo toggle action on a user that has held akimbo-capable guns.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedAkimboSystem))]
public sealed partial class AkimboUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    /// The akimbo gun that was fired most recently. The next trigger pull goes to the other one.
    /// Not saved, as it only matters mid-fight.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? LastFired;
}
