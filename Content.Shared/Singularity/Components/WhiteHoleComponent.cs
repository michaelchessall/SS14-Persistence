using Robust.Shared.GameStates;

namespace Content.Shared.Singularity.Components;

/// <summary>
/// A white hole that is linked to a singularity and ejects everything the singularity consumes.
/// Spawns at the opposite coordinates and maintains that position.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WhiteHoleComponent : Component
{
    /// <summary>
    /// The UID of the singularity this white hole is linked to.
    /// </summary>
    [DataField("linkedSingularity")]
    public EntityUid? LinkedSingularity;

    /// <summary>
    /// The speed at which entities are ejected from the white hole.
    /// </summary>
    [DataField("ejectSpeed")]
    public float EjectSpeed = 10f;

    /// <summary>
    /// Whether this white hole should follow the singularity or stay at its spawned location.
    /// </summary>
    [DataField("followSingularity")]
    public bool FollowSingularity = true;
}
