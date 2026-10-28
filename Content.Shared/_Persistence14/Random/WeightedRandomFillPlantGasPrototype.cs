using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Random;

/// <summary>
///     Random weighting dataset for plant chemicals, able to specify chemicals quantity.
/// </summary>
[Prototype]
public sealed partial class WeightedRandomFillPlantGasPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    ///     List of RandomFills that can be picked from.
    /// </summary>
    [DataField(required: true)]
    public List<RandomFillPlantGas> Fills = new();
}
