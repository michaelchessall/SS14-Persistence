using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Marks an entity as visible on shuttle/radar navigation screens.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NavScreenTrackableComponent : Component
{
    /// <summary>
    /// Optional fixed label shown on the nav display. Falls back to entity name when empty.
    /// </summary>
    [DataField]
    public string? Label;

    /// <summary>
    /// Marker color used on the nav display.
    /// </summary>
    [DataField]
    public Color Color = Color.OrangeRed;

    /// <summary>
    /// Controls how this tracker is visualized on the nav screen.
    /// </summary>
    [DataField]
    public NavScreenTrackerType TrackerType = NavScreenTrackerType.Standard;

    /// <summary>
    /// Base marker radius in nav-screen pixels.
    /// </summary>
    [DataField]
    public float MarkerSize = 4f;

    /// <summary>
    /// Additional marker radius applied per singularity level above 1.
    /// Only used when this entity has a <see cref="Content.Shared.Singularity.Components.SingularityComponent"/>.
    /// </summary>
    [DataField]
    public float SingularityLevelSizeGrowth = 1f;

    /// <summary>
    /// If true, the label text will be rendered next to the marker.
    /// </summary>
    [DataField]
    public bool ShowLabel = true;
}

[Serializable, NetSerializable]
public enum NavScreenTrackerType : byte
{
    Standard,
    SpawnTracked,
}
