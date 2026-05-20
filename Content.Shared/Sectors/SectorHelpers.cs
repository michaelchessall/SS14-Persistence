using System.Numerics;
using Robust.Shared.Maths;

namespace Content.Shared.Sectors;

/// <summary>
/// Shared sector math helpers for classifying world positions into compass sectors.
/// </summary>
public static class SectorHelpers
{
    public const float EastLowerBoundary = 22.5f;
    public const float NorthEastUpperBoundary = 67.5f;
    public const float NorthUpperBoundary = 112.5f;
    public const float NorthWestUpperBoundary = 157.5f;
    public const float WestUpperBoundary = 202.5f;
    public const float SouthWestUpperBoundary = 247.5f;
    public const float SouthUpperBoundary = 292.5f;
    public const float SouthEastUpperBoundary = 337.5f;

    /// <summary>
    /// Classifies a world position into a space sector relative to the fixed origin.
    /// </summary>
    public static SpaceSector GetSector(Vector2 worldPos, float centerRadius)
    {
        if (worldPos.Length() <= centerRadius)
            return SpaceSector.Center;

        var angleRadians = MathF.Atan2(worldPos.Y, worldPos.X);
        var degrees = (MathHelper.RadiansToDegrees(angleRadians) + 360f) % 360f;

        if (degrees >= SouthEastUpperBoundary || degrees < EastLowerBoundary)
            return SpaceSector.East;

        if (degrees < NorthEastUpperBoundary)
            return SpaceSector.NorthEast;

        if (degrees < NorthUpperBoundary)
            return SpaceSector.North;

        if (degrees < NorthWestUpperBoundary)
            return SpaceSector.NorthWest;

        if (degrees < WestUpperBoundary)
            return SpaceSector.West;

        if (degrees < SouthWestUpperBoundary)
            return SpaceSector.SouthWest;

        if (degrees < SouthUpperBoundary)
            return SpaceSector.South;

        return SpaceSector.SouthEast;
    }
}