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
}