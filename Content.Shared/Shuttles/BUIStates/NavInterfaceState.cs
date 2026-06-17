using Content.Shared.Sectors;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class NavInterfaceState
{
    public float MaxRange;

    /// <summary>
    /// The relevant coordinates to base the radar around.
    /// </summary>
    public NetCoordinates? Coordinates;

    /// <summary>
    /// The relevant rotation to rotate the angle around.
    /// </summary>
    public Angle? Angle;

    public Dictionary<NetEntity, List<DockingPortState>> Docks;

    public Dictionary<SpaceSector, string> SectorWeatherEvents;

    public List<NavTrackedEntityState> TrackedEntities;

    public bool RotateWithEntity = true;

    public readonly ShuttleDampingMode DampingMode;

    public NavInterfaceState(
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        Dictionary<NetEntity, List<DockingPortState>> docks,
        ShuttleDampingMode dampingMode,
        Dictionary<SpaceSector, string>? sectorWeatherEvents = null,
        List<NavTrackedEntityState>? trackedEntities = null)
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Docks = docks;
        DampingMode = dampingMode;
        SectorWeatherEvents = sectorWeatherEvents ?? new Dictionary<SpaceSector, string>();
        TrackedEntities = trackedEntities ?? new List<NavTrackedEntityState>();
    }
}

[Serializable, NetSerializable]
public sealed class NavTrackedEntityState
{
    public NetEntity Entity;
    public NetCoordinates Coordinates;
    public Color Color;
    public string Label;
    public bool ShowLabel;
    public NavScreenTrackerType TrackerType;
    public NetCoordinates? SpawnCoordinates;
    public float MarkerSize;

    public NavTrackedEntityState(
        NetEntity entity,
        NetCoordinates coordinates,
        Color color,
        string label,
        bool showLabel,
        NavScreenTrackerType trackerType,
        NetCoordinates? spawnCoordinates,
        float markerSize)
    {
        Entity = entity;
        Coordinates = coordinates;
        Color = color;
        Label = label;
        ShowLabel = showLabel;
        TrackerType = trackerType;
        SpawnCoordinates = spawnCoordinates;
        MarkerSize = markerSize;
    }
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}
