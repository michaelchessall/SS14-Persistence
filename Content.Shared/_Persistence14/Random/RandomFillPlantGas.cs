using Content.Shared._Persistence14.Botany;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.Random;

/// <summary>
///     Data that specifies plant chemicals that share the same weight and quantity for use with WeightedRandomPlantChemical.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RandomFillPlantGas
{
    /// <summary>
    ///     Quantity of listed reagents.
    /// </summary>
    [DataField("quantityId")]
    public ProtoId<PlantGasQuantityPrototype> Quantity;

    /// <summary>
    ///     Random weight of listed reagents.
    /// </summary>
    [DataField("weight")]
    public float Weight = 0;

    /// <summary>
    ///     Listed gasses that the weight and quantity apply to.
    /// </summary>
    [DataField(required: true)]
    public List<Gas> Gasses = new();
}
