using Content.Shared._Persistence14.Botany;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.Random;

/// <summary>
///     Data that specifies plant chemicals that share the same weight and quantity for use with WeightedRandomPlantChemical.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RandomFillPlantChemical
{
    /// <summary>
    ///     Quantity of listed reagents.
    /// </summary>
    [DataField("quantityId")]
    public ProtoId<PlantChemQuantityPrototype> Quantity;

    /// <summary>
    ///     Random weight of listed reagents.
    /// </summary>
    [DataField("weight")]
    public float Weight = 0;

    /// <summary>
    ///     Listed reagents that the weight and quantity apply to.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<ReagentPrototype>> Reagents = new();
}
