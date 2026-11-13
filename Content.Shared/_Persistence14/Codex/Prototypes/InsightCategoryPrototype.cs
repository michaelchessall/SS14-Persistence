using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Codex.Prototypes;

[Prototype]
public sealed partial class InsightCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public string Name = "";
    [DataField]
    public int Order = 0;
    [DataField]
    public ProtoId<InsightTraitPrototype> FirstTrait = "";
    [DataField]
    public Color Color = Color.Red;

}
