using Content.Shared.FixedPoint;

namespace Content.Server.Botany;

[Serializable]
[DataDefinition]
public sealed partial class PlantNutrientInfo
{
    [DataField]
    public FixedPoint2 Amount = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Required = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Bonus = FixedPoint2.Zero;
}
