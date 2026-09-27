using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Persistence14.RandomTable.Selectors;

public sealed partial class RandomTableNestedSelector : RandomTableSelector
{
    [DataField(required: true)]
    public ProtoId<RandomTablePrototype> TableId = default!;

    [DataField]
    private NestedSelectorWeightMode _weightMode = NestedSelectorWeightMode.Manual;

    public RandomTableNestedSelector(ProtoId<RandomTablePrototype> tableId, NestedSelectorWeightMode weightMode = NestedSelectorWeightMode.Manual)
    {
        TableId = tableId;
        _weightMode = weightMode;
    }

    protected override IEnumerable<RandomTableValueDefinition> RunImplementation(RandomTableContext ctx)
    {
        var table = ctx.PrototypeManager.Index(TableId).Table;

        foreach (var item in table.Run(ctx))
            yield return item;
    }

    public override IEnumerable<(RandomTableValueDefinition value, float prob)> List(RandomTableContext ctx, float probabilityMultipler = 1f)
    {
        var table = ctx.PrototypeManager.Index(TableId).Table;

        foreach (var (value, prob) in table.List(ctx, probabilityMultipler))
            yield return (value, prob);
    }

    public override float GetWeight(RandomTableContext ctx)
    {
        switch (_weightMode)
        {
            case NestedSelectorWeightMode.Manual:
                return base.GetWeight(ctx);

            case NestedSelectorWeightMode.Inherit:
                var table = ctx.PrototypeManager.Index(TableId).Table;
                return table.GetWeight(ctx) * base.GetWeight(ctx);

            case NestedSelectorWeightMode.SumChildren:
                table = ctx.PrototypeManager.Index(TableId).Table;
                if (table is not IRandomTableCollectionSelector collection)
                {
                    ctx.LogManager
                        .GetSawmill("random-table-nested-selector")
                        .Warning("Attempted to SumChildren on non-collection table. Defaulting to manual selection mode.");
                    return base.GetWeight(ctx);
                }
                var sum = 0f;
                foreach (var child in collection.GetChildren(ctx))
                    sum += child.GetWeight(ctx);
                return sum * base.GetWeight(ctx);
        }

        return base.GetWeight(ctx);
    }

    public enum NestedSelectorWeightMode
    {
        Manual,
        Inherit,
        SumChildren
    }
}