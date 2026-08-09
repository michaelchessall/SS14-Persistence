using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.EntityEffects;

public sealed partial class RevertPolymorph : EntityEffectBase<RevertPolymorph>
{
    [DataField]
    public string EffectGuidebookLoc = "entity-effect-guidebook-revert-polymorph";
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(EffectGuidebookLoc);
}