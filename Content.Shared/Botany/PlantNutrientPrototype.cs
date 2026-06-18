using Robust.Shared.Prototypes;

namespace Content.Shared.Botany;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype]
[DataDefinition]
public sealed partial class PlantNutrientPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    private LocId Name { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField("desc", required: true)]
    private LocId Description { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDescription => Loc.GetString(Description);

    [DataField("color")]
    public Color SubstanceColor { get; private set; } = Color.White;
}
