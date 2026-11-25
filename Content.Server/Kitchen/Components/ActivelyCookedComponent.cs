namespace Content.Server.Kitchen.Components;

/// <summary>
/// Attached to an object that's actively being cooked. Modified by Persi14
/// </summary>
[RegisterComponent]
public sealed partial class ActivelyCookedComponent : Component
{
    /// <summary>
    /// The microwave this entity is actively being microwaved by.
    /// </summary>
    [DataField]
    public EntityUid? Microwave;
}
