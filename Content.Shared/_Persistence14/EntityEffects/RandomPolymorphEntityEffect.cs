using Content.Shared._Persistence14.RandomTable;
using Content.Shared._Persistence14.RandomTable.Selectors;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.EntityEffects;

public sealed partial class RandomPolymorph : EntityEffectBase<RandomPolymorph>
{
    [DataField(required: true)]
    public RandomTableSelector Table = new RandomTableNullSelector();

    [DataField]
    public bool ThrowIfMultiple = true;

    [DataField]
    public string EffectGuidebookLoc = "entity-effect-guidebook-random-polymorph";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(EffectGuidebookLoc);
}