using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Atmos.Piping.Binary;

/// <summary>
/// Prototype for gas recycling recipe definitions.
/// Defines which gases can be recycled, under what conditions, what outputs are produced,
/// and what reagents can be scrubbed from the gas into containers.
/// </summary>
[Prototype]
public sealed partial class GasRecyclingRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The input gas to be recycled.
    /// </summary>
    [DataField("inputGas", required: true)]
    public Gas InputGas { get; private set; } = Gas.Oxygen;

    /// <summary>
    /// The output gas produced by recycling.
    /// </summary>
    [DataField("outputGas", required: true)]
    public Gas OutputGas { get; private set; } = Gas.Oxygen;

    /// <summary>
    /// The conversion ratio from input to output (e.g., 1.0 means 1:1 conversion).
    /// </summary>
    [DataField("conversionRatio")]
    public float ConversionRatio { get; private set; } = 1.0f;

    /// <summary>
    /// Minimum temperature required for recycling to occur (in Kelvin).
    /// </summary>
    [DataField("minTemp")]
    public float MinimumTemperature { get; private set; } = Atmospherics.T20C;

    /// <summary>
    /// Minimum pressure required for recycling to occur (in kPa).
    /// </summary>
    [DataField("minPressure")]
    public float MinimumPressure { get; private set; } = Atmospherics.OneAtmosphere;

    /// <summary>
    /// Reagents that can be scrubbed from the gas into containers.
    /// Maps reagent IDs to their conversion ratios (how much reagent is produced per unit of input gas).
    /// </summary>
    [DataField("scrubbedReagents", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, ReagentPrototype>))]
    public Dictionary<string, float> ScrubbedReagents { get; private set; } = new();

    /// <summary>
    /// Whether this recipe is enabled by default.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled { get; private set; } = true;
}
