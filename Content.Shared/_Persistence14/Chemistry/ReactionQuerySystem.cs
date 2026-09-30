using System.Collections.Frozen;
using System.Linq;
using Content.Shared._Persistence14.Query;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Chemistry;

public sealed partial class ReactionQuerySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private FrozenDictionary<ProtoId<ReagentPrototype>, List<ReactionPrototype>> _byReactant = default!;
    private FrozenDictionary<ProtoId<ReagentPrototype>, List<ReactionPrototype>> _byProduct = default!;
    private FrozenDictionary<ProtoId<MixingCategoryPrototype>, List<ReactionPrototype>> _byMixingCategory = default!;
    private FrozenSet<ReactionPrototype> _allReactions = default!;

    public override void Initialize()
    {
        InitializeQuery();
    }

    /// <summary>
    /// Initializes all frozen dictionaries based on a query of the <see cref="ReactionPrototype"/>s.
    /// </summary>
    public void InitializeQuery()
    {
        var allReactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        _allReactions = allReactions.ToFrozenSet();

        var byReactant = new Dictionary<ProtoId<ReagentPrototype>, List<ReactionPrototype>>();
        var byProduct = new Dictionary<ProtoId<ReagentPrototype>, List<ReactionPrototype>>();
        var byMixingCategory = new Dictionary<ProtoId<MixingCategoryPrototype>, List<ReactionPrototype>>();

        foreach (var reaction in allReactions)
        {
            foreach (var reactant in reaction.Reactants.Keys)
                PushToDictionary(byReactant, reactant, reaction);

            foreach (var product in reaction.Products.Keys)
                PushToDictionary(byProduct, product, reaction);

            if (reaction.MixingCategories is { } categories)
                foreach (var category in categories)
                    PushToDictionary(byMixingCategory, category, reaction);
        }

        _byReactant = byReactant.ToFrozenDictionary();
        _byProduct = byProduct.ToFrozenDictionary();
        _byMixingCategory = byMixingCategory.ToFrozenDictionary();
    }

    private void PushToDictionary<TKey, TData>(Dictionary<TKey, List<TData>> dictionary, TKey key, TData data) where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var currList))
        {
            currList.Add(data);
            return;
        }
        dictionary.Add(key, new List<TData> { data });
    }

    private IEnumerable<ReactionPrototype> GetReactionsBy<TKey>(IReadOnlyDictionary<TKey, List<ReactionPrototype>> dictionary, TKey key) where TKey : notnull
        => dictionary.TryGetValue(key, out var reactions) ? reactions : [];
    /// <summary>
    /// Returns all reactions containing the specified reagent(s) as reactants. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByReactant(ProtoId<ReagentPrototype> reactant)
        => GetReactionsBy(_byReactant, reactant);
    /// <summary>
    /// Returns all reactions containing the specified reagent(s) as products. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByProduct(ProtoId<ReagentPrototype> product)
        => GetReactionsBy(_byProduct, product);
    /// <summary>
    /// Returns all reactions containing the specified mixing categories. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByMixingCategory(ProtoId<MixingCategoryPrototype> mixingCategory)
        => GetReactionsBy(_byMixingCategory, mixingCategory);

    private IEnumerable<ReactionPrototype> GetReactionsBy<TKey>(IReadOnlyDictionary<TKey, List<ReactionPrototype>> dictionary, QueryMode mode, params TKey[] keys) where TKey : notnull
    {
        if (keys.Length == 0)
            return [];

        var reactions = keys
            .Distinct()
            .Select(key => dictionary.TryGetValue(key, out var values)
                ? values.AsEnumerable()
                : []);

        return mode switch
        {
            QueryMode.Any => reactions
                .SelectMany(x => x)
                .Distinct(),

            QueryMode.All => reactions
                .Aggregate((current, next) => current.Intersect(next)),

            _ => [],
        };
    }
    /// <summary>
    /// Returns all reactions containing the specified reagent(s) as reactants. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByReactant(params ProtoId<ReagentPrototype>[] reactants)
        => GetReactionsBy(_byReactant, QueryMode.All, reactants);
    /// <summary>
    /// Returns all reactions containing the specified reagent(s) as reactants. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByReactant(QueryMode mode, params ProtoId<ReagentPrototype>[] reactants)
        => GetReactionsBy(_byReactant, mode, reactants);
    /// <summary>
    /// Returns all reactions containing the specified mixing categories. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByProduct(params ProtoId<ReagentPrototype>[] product)
        => GetReactionsBy(_byProduct, QueryMode.All, product);
    /// <summary>
    /// Returns all reactions containing the specified mixing categories. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByProduct(QueryMode mode, params ProtoId<ReagentPrototype>[] product)
        => GetReactionsBy(_byProduct, mode, product);
    /// <summary>
    /// Returns all reactions containing the specified mixing categories. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByMixingCategory(params ProtoId<MixingCategoryPrototype>[] mixingCategory)
        => GetReactionsBy(_byMixingCategory, QueryMode.All, mixingCategory);
    /// <summary>
    /// Returns all reactions containing the specified mixing categories. May be configured to return based on an All or Any selection criteria.
    /// </summary>
    public IEnumerable<ReactionPrototype> GetReactionsByMixingCategory(QueryMode mode, params ProtoId<MixingCategoryPrototype>[] mixingCategory)
        => GetReactionsBy(_byMixingCategory, mode, mixingCategory);

    public IEnumerable<ReactionPrototype> GetAll() => _allReactions;
    public IEnumerable<ReactionPrototype> GetWhere(Func<ReactionPrototype, bool> condition)
    {
        foreach (var reaction in _allReactions)
        {
            if (condition(reaction))
                yield return reaction;
        }
    }
}