using Content.Shared._Persistence14.RandomTable.ValueDefinition;

namespace Content.Shared._Persistence14.RandomTable.Count;

public sealed partial class ConstantCountSelector : CountSelector
{
    [DataField]
    public int Value = 1;

    /// <inheritdoc/>
    public override int Get(RandomTableContext ctx) => Value;
    /// <inheritdoc/>
    public override float Probability(RandomTableContext ctx, int threshold = 1) => Value >= threshold ? 1f : 0f;
    /// <inheritdoc/>
    public override float Average(RandomTableContext ctx) => Value;

    public ConstantCountSelector(int value)
    {
        Value = value;
    }
    public ConstantCountSelector() { }
}