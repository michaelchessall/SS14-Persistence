using Content.Server.Sectors.Systems;
using Content.Shared.Sectors;

namespace Content.Server.Sectors.Components;

/// <summary>
/// Stores active sector weather state on a map entity so map save/load preserves it.
/// </summary>
[RegisterComponent, Access(typeof(SectorWeatherSystem))]
public sealed partial class SectorWeatherPersistenceComponent : Component
{
    [DataField]
    public Dictionary<SpaceSector, string> ActiveWeather = new();
}
