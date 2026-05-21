using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared.Sectors.Prototypes;

[Prototype("sectorWeather")]
public sealed partial class SectorWeatherPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField(required: true)]
    public Color BorderColor { get; private set; } = Color.White;

    [DataField]
    public bool Hazard { get; private set; } = false;

    [DataField]
    public Color ScreenTintColor { get; private set; } = Color.Transparent;

    [DataField]
    public float ScreenTintStrength { get; private set; } = 1f;

    [DataField]
    public float ScreenTintNoiseStrength { get; private set; } = 0f;

    [DataField]
    public string? Parallax;

    [DataField]
    public List<SectorWeatherSpawnEntry> Spawns { get; private set; } = new();
}

[DataDefinition]
public sealed partial class SectorWeatherSpawnEntry
{
    [DataField(required: true)]
    public EntProtoId Prototype = string.Empty;

    [DataField]
    public float SpawnInterval = 30f;

    [DataField]
    public int SpawnAttempts = 8;

    [DataField]
    public int MaxActive = 0;

    [DataField]
    public int SectorMinCount = 0;

    [DataField]
    public int SectorMaxCount = 0;

    [DataField]
    public float SpawnChanceAboveSectorMin = 1f;

    [DataField]
    public float MinSpawnRange = 64f;

    [DataField]
    public float MaxSpawnRange = 256f;

    [DataField]
    public float MinDistanceFromPoweredEntity = 0f;

    [DataField]
    public float MinDistanceFromSamePrototype = 0f;

    [DataField]
    public byte? MinSpawnLevel;

    [DataField]
    public byte? MaxSpawnLevel;

    [DataField]
    public float? DespawnDistanceFromSpawn;
}
