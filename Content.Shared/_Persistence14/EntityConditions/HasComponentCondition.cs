using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.EntityConditions;

public sealed partial class HasComponent : EntityConditionBase<HasComponent>
{
    [DataField(required: true)]
    public string Component = string.Empty;

    [DataField]
    public string GuidebookTextLoc = "entity-condition-has-component";

    /// <inheritdoc/>
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString(GuidebookTextLoc, ("inverted", Inverted), ("component", Component));
}