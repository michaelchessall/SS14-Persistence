using System.Numerics;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared.Sectors;

/// <summary>
/// Shared API for resolving named sectors from world positions and entities.
/// </summary>
public sealed class SharedSectorSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private float CenterRadius => _configuration.GetCVar(CCVars.SectorCenterRadius);

    /// <summary>
    /// Gets the space sector for the provided world position.
    /// </summary>
    public SpaceSector GetSector(Vector2 worldPos)
    {
        return SectorHelpers.GetSector(worldPos, CenterRadius);
    }

    /// <summary>
    /// Gets the space sector for an entity's world position.
    /// </summary>
    public SpaceSector GetSector(EntityUid uid)
    {
        return GetSector(_transform.GetWorldPosition(uid));
    }

    /// <summary>
    /// Tries to resolve the space sector for an entity.
    /// </summary>
    public bool TryGetSector(EntityUid uid, out SpaceSector sector)
    {
        if (!Exists(uid))
        {
            sector = default;
            return false;
        }

        sector = GetSector(uid);
        return true;
    }

    /// <summary>
    /// Gets the localized display name for a sector.
    /// </summary>
    public string GetSectorName(SpaceSector sector)
    {
        var overrideName = _configuration.GetCVar(GetSectorNameCVar(sector));
        if (!string.IsNullOrWhiteSpace(overrideName))
            return overrideName;

        return Loc.GetString(sector switch
        {
            SpaceSector.Center => "sector-center",
            SpaceSector.North => "sector-north",
            SpaceSector.NorthEast => "sector-northeast",
            SpaceSector.East => "sector-east",
            SpaceSector.SouthEast => "sector-southeast",
            SpaceSector.South => "sector-south",
            SpaceSector.SouthWest => "sector-southwest",
            SpaceSector.West => "sector-west",
            SpaceSector.NorthWest => "sector-northwest",
            _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, null),
        });
    }

    public static CVarDef<string> GetSectorNameCVar(SpaceSector sector)
    {
        return sector switch
        {
            SpaceSector.Center => CCVars.SectorNameCenter,
            SpaceSector.North => CCVars.SectorNameNorth,
            SpaceSector.NorthEast => CCVars.SectorNameNorthEast,
            SpaceSector.East => CCVars.SectorNameEast,
            SpaceSector.SouthEast => CCVars.SectorNameSouthEast,
            SpaceSector.South => CCVars.SectorNameSouth,
            SpaceSector.SouthWest => CCVars.SectorNameSouthWest,
            SpaceSector.West => CCVars.SectorNameWest,
            SpaceSector.NorthWest => CCVars.SectorNameNorthWest,
            _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, null),
        };
    }
}