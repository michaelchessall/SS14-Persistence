using Content.Shared.Sectors;
using Robust.Shared.Serialization;

namespace Content.Shared.Sectors.Events;

[Serializable, NetSerializable]
public sealed class SectorWeatherStateUpdateEvent : EntityEventArgs
{
    public readonly Dictionary<SpaceSector, string> ActiveWeather;

    public SectorWeatherStateUpdateEvent(Dictionary<SpaceSector, string> activeWeather)
    {
        ActiveWeather = activeWeather;
    }
}