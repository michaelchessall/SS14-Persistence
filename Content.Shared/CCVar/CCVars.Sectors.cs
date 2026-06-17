using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Radius around the world origin that resolves to the center sector.
    /// </summary>
    public static readonly CVarDef<float> SectorCenterRadius =
        CVarDef.Create("sector.center_radius", 1250f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// Reserved maximum radius for future sector-based systems.
    /// </summary>
    public static readonly CVarDef<float> SectorMaxRadius =
        CVarDef.Create("sector.max_radius", 50000f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// Optional display name overrides for each sector. Empty string uses localization defaults.
    /// </summary>
    public static readonly CVarDef<string> SectorNameCenter =
        CVarDef.Create("sector.name.center", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
    public static readonly CVarDef<string> SectorNameNorth =
        CVarDef.Create("sector.name.north", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
    public static readonly CVarDef<string> SectorNameNorthEast =
        CVarDef.Create("sector.name.northeast", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
    public static readonly CVarDef<string> SectorNameEast =
        CVarDef.Create("sector.name.east", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
    public static readonly CVarDef<string> SectorNameSouthEast =
        CVarDef.Create("sector.name.southeast", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
    public static readonly CVarDef<string> SectorNameSouth =
        CVarDef.Create("sector.name.south", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
    public static readonly CVarDef<string> SectorNameSouthWest =
        CVarDef.Create("sector.name.southwest", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
    public static readonly CVarDef<string> SectorNameWest =
        CVarDef.Create("sector.name.west", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
    public static readonly CVarDef<string> SectorNameNorthWest =
        CVarDef.Create("sector.name.northwest", string.Empty, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// Persisted active weather prototype IDs for each sector. Empty string means no active weather.
    /// </summary>
    public static readonly CVarDef<string> SectorWeatherCenter =
        CVarDef.Create("sector.weather.center", string.Empty, CVar.ARCHIVE | CVar.SERVER);
    public static readonly CVarDef<string> SectorWeatherNorth =
        CVarDef.Create("sector.weather.north", string.Empty, CVar.ARCHIVE | CVar.SERVER);
    public static readonly CVarDef<string> SectorWeatherNorthEast =
        CVarDef.Create("sector.weather.northeast", string.Empty, CVar.ARCHIVE | CVar.SERVER);
    public static readonly CVarDef<string> SectorWeatherEast =
        CVarDef.Create("sector.weather.east", string.Empty, CVar.ARCHIVE | CVar.SERVER);
    public static readonly CVarDef<string> SectorWeatherSouthEast =
        CVarDef.Create("sector.weather.southeast", string.Empty, CVar.ARCHIVE | CVar.SERVER);
    public static readonly CVarDef<string> SectorWeatherSouth =
        CVarDef.Create("sector.weather.south", string.Empty, CVar.ARCHIVE | CVar.SERVER);
    public static readonly CVarDef<string> SectorWeatherSouthWest =
        CVarDef.Create("sector.weather.southwest", string.Empty, CVar.ARCHIVE | CVar.SERVER);
    public static readonly CVarDef<string> SectorWeatherWest =
        CVarDef.Create("sector.weather.west", string.Empty, CVar.ARCHIVE | CVar.SERVER);
    public static readonly CVarDef<string> SectorWeatherNorthWest =
        CVarDef.Create("sector.weather.northwest", string.Empty, CVar.ARCHIVE | CVar.SERVER);
}