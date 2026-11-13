namespace Content.Server._Persistence14.Shuttles;

/// <summary>
/// A simpler implementation of the station anchor component that does not require power. When the entity is anchored, it anchors the grid. That simple.
/// Also prevents unanchoring, though that should really be done through the AnchorableComponent
/// </summary>
[RegisterComponent, Access(typeof(SimpleStationAnchorSystem))]
public sealed partial class SimpleStationAnchorComponent : Component
{
    /// <summary>
    /// If true, anchor will prevent any unanchoring attempt, providing the same message as the traditional StationAnchorSystem.
    /// If this is always true, you should *really* be using the AnchorableComponent to handle this.
    /// </summary>
    [DataField]
    public bool PreventUnanchoring = true;
}