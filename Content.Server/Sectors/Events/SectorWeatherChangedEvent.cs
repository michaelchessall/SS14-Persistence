using Content.Shared.Sectors;

namespace Content.Server.Sectors.Events;

public sealed class SectorWeatherChangedEvent : EntityEventArgs
{
    public SpaceSector Sector { get; }
    public string? WeatherId { get; }

    public SectorWeatherChangedEvent(SpaceSector sector, string? weatherId)
    {
        Sector = sector;
        WeatherId = weatherId;
    }
}
