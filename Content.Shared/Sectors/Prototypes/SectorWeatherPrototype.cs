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
}
