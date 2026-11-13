using System.Linq;
using Content.Shared._Persistence14.Query;
using Content.Shared._Persistence14.Whitelist;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Chemistry;

public sealed partial class ReagentWhitelistSystem : BaseWhitelistSystem<ReagentWhitelist, ProtoId<ReagentPrototype>>
{
    [Dependency] private ReactionQuerySystem _reactionQuery = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override bool WhitelistPass(ReagentWhitelist? whitelist, ProtoId<ReagentPrototype> reagentId)
    {
        if (whitelist is not { } white)
            return false;
        var reagent = _prototypeManager.Index(reagentId);

        if (!PassReagent(white, reagent) ||
            !PassTags(white, reagent) ||
            !PassGroups(white, reagent) ||
            !PassReactants(white, reagent) ||
            !PassMixingCategories(white, reagent))
            return false;

        return true;
    }
    public override bool WhitelistFail(ReagentWhitelist? whitelist, ProtoId<ReagentPrototype> reagentId)
    {
        if (whitelist is not { } white)
            return false;
        var reagent = _prototypeManager.Index(reagentId);

        if (!PassReagent(white, reagent) ||
            !PassTags(white, reagent) ||
            !PassGroups(white, reagent) ||
            !PassReactants(white, reagent) ||
            !PassMixingCategories(white, reagent))
            return true;

        return false;
    }

    #region Partial Pass Methods
    private bool PassGeneric<TData>(ReagentPrototype reagent, TData[] data, QueryMode mode, Func<TData, ReagentPrototype, bool> condition)
    {
        if (data.Length == 0) return true;
        foreach (var d in data)
        {
            var c = condition(d, reagent);
            if (c && mode == QueryMode.Any)
                return true;
            if (!c && mode == QueryMode.All)
                return false;
        }
        return mode == QueryMode.All;
    }

    private bool PassReagent(ReagentWhitelist whitelist, ReagentPrototype reagent)
        => PassGeneric(reagent, whitelist.Reagents.ToArray(), QueryMode.Any, (d, r) => d == r.ID);
    private bool PassGroups(ReagentWhitelist whitelist, ReagentPrototype reagent)
        => PassGeneric(reagent, whitelist.ReactiveGroups.ToArray(), whitelist.ReactiveGroupSelectionMode, (d, r) => r.ReactiveEffects?.ContainsKey(d) ?? false);
    private bool PassTags(ReagentWhitelist whitelist, ReagentPrototype reagent)
        => PassGeneric(reagent, whitelist.Tags.ToArray(), whitelist.TagSelectionMode, (d, r) => r.Tags.Contains(d));
    private bool PassReactants(ReagentWhitelist whitelist, ReagentPrototype reagent)
    {
        if (whitelist.FromReactants.Count == 0)
            return true;

        var reactions = _reactionQuery.GetReactionsByProduct(reagent)
                .Intersect(_reactionQuery.GetReactionsByReactant(whitelist.ReactantSelectionMode, whitelist.FromReactants.ToArray()));

        return reactions.Any();
    }
    private bool PassMixingCategories(ReagentWhitelist whitelist, ReagentPrototype reagent)
    {
        if (whitelist.ReactionMixingCategories.Count == 0)
            return true;

        var reactions = _reactionQuery.GetReactionsByProduct(reagent)
                .Intersect(_reactionQuery.GetReactionsByMixingCategory(whitelist.CategorySelectionMode, whitelist.ReactionMixingCategories.ToArray()));

        return reactions.Any();
    }
    #endregion
}

[DataDefinition]
public sealed partial class ReagentWhitelist
{
    [DataField("reagents")]
    public HashSet<ProtoId<ReagentPrototype>> Reagents = new();

    [DataField("groups")]
    public HashSet<ProtoId<ReactiveGroupPrototype>> ReactiveGroups = new();
    [DataField]
    public QueryMode ReactiveGroupSelectionMode = QueryMode.Any;

    [DataField("tags")]
    public HashSet<ProtoId<TagPrototype>> Tags = new();
    [DataField]
    public QueryMode TagSelectionMode = QueryMode.Any;

    [DataField("reactants")]
    public HashSet<ProtoId<ReagentPrototype>> FromReactants = new();
    [DataField]
    public QueryMode ReactantSelectionMode = QueryMode.Any;

    [DataField("mixingCategories")]
    public HashSet<ProtoId<MixingCategoryPrototype>> ReactionMixingCategories = new();
    [DataField]
    public QueryMode CategorySelectionMode = QueryMode.Any;
}