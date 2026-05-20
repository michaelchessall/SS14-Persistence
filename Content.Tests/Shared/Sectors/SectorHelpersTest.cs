using System.Numerics;
using Content.Shared.Sectors;
using NUnit.Framework;
using Robust.UnitTesting;

namespace Content.Tests.Shared.Sectors;

[TestFixture]
public sealed class SectorHelpersTest : RobustUnitTest
{
    private const float CenterRadius = 1250f;

    [TestCase(0f, 0f, SpaceSector.Center)]
    [TestCase(10000f, 0f, SpaceSector.East)]
    [TestCase(-10000f, 0f, SpaceSector.West)]
    [TestCase(0f, 10000f, SpaceSector.North)]
    [TestCase(0f, -10000f, SpaceSector.South)]
    [TestCase(10000f, 10000f, SpaceSector.NorthEast)]
    [TestCase(-10000f, 10000f, SpaceSector.NorthWest)]
    [TestCase(10000f, -10000f, SpaceSector.SouthEast)]
    [TestCase(-10000f, -10000f, SpaceSector.SouthWest)]
    [TestCase(1250f, 0f, SpaceSector.Center)]
    [TestCase(1250.1f, 0f, SpaceSector.East)]
    public void GetSector_ReturnsExpectedSector(float x, float y, SpaceSector expected)
    {
        var sector = SectorHelpers.GetSector(new Vector2(x, y), CenterRadius);

        Assert.That(sector, Is.EqualTo(expected));
    }
}