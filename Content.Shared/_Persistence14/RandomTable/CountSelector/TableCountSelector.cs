using System.Linq;
using Content.Shared._Persistence14.RandomTable.ValueDefinition;
using Robust.Shared.Random;

namespace Content.Shared._Persistence14.RandomTable.Count;

public sealed partial class TableCountSelector : CountSelector
{
    [DataField]
    public RandomTableSelector Table = (RandomTableIntValueDefinition)1;

    public override int Get(RandomTableContext ctx)
    {
        foreach (var item in ctx.RandomTableSystem.RunInt(Table, ctx))
            return item;
        return 0;
    }

    /// <inheritdoc/>
    public override float Average(RandomTableContext ctx)
    {
        float sum = 0f;
        foreach (var (val, prob) in ctx.RandomTableSystem.ListInt(Table, ctx))
        {
            sum += val * prob;
        }
        return sum;
    }

    /// <inheritdoc/>
    public override float Probability(RandomTableContext ctx, int threshold = 1)
    {
        float sumGreater = 0f;

        foreach (var (val, prob) in ctx.RandomTableSystem.ListInt(Table, ctx))
        {
            if (val >= threshold) sumGreater += prob;
        }

        return sumGreater;
    }
}