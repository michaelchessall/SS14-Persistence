using Robust.Shared.Prototypes;

namespace Content.Server.Botany;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype()]
public sealed partial class PlantNutrientPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    private LocId Name { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);
}
