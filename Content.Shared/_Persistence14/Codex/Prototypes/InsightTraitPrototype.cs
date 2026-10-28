using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Codex.Prototypes;


[Prototype]
public sealed partial class InsightTraitPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField(required: true)]
    public LocId Name = "";
    [DataField]
    public LocId? Content = null;
    [DataField]
    public ProtoId<InsightTraitPrototype>? Next = null;
    [DataField(required: true)]
    public ProtoId<TraitPrototype> Trait = "";
    [DataField]
    public bool RequiresAwake = false;
}
