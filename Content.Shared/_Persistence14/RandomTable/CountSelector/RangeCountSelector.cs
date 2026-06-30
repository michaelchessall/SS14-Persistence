using Content.Shared._Persistence14.RandomTable.ValueDefinition;
using Robust.Shared.Random;

namespace Content.Shared._Persistence14.RandomTable.Count;

public sealed partial class RangeCountSelector : CountSelector
{
    [DataField("min", required: true)]
    private int _minimumCount = 1;

    /// <summary>
    /// If true, the range will include the minumum value.
    /// </summary>
    [DataField]
    public bool MinInclusive = true;

    [DataField("max", required: true)]
    private int _maximumCount = 1;

    /// <summary>
    /// If true, the range will include the maximum value.
    /// </summary>
    [DataField]
    public bool MaxInclusive = true;

    /// <summary>
    /// The true maximum value when considering the state of <see cref="MaxInclusive"/>
    /// </summary>
    public int Max => _maximumCount - (MaxInclusive ? 0 : 1);
    /// <summary>
    /// The true minimum value when considering the state of <see cref="MinInclusive"/> 
    /// </summary>
    public int Min => _minimumCount + (MinInclusive ? 0 : 1);
    /// <summary>
    /// The number of valid counts in the selector.
    /// </summary>
    public int Range => Max - Min + 1;

    /// <inheritdoc/>
    public override int Get(RandomTableContext ctx) => Range <= 0 ? 0 : ctx.Random.Next(Min, Max + 1);
    /// <inheritdoc/>
    public override float Probability(RandomTableContext ctx, int threshold = 1)
    {
        if (Range < 1) return 0f;
        if (Range == 1)
        {
            var singleValue = Min;
            return threshold <= singleValue ? 1f : 0f;
        }
        if (threshold < Min) return 1f; // Smaller than the minimum, definitely true.
        if (threshold > Max) return 0f; // Bigger than the maximum, definitely false.

        return (Max - threshold + 1) / (float)Range;
    }

    /// <inheritdoc/>
    public override float Average(RandomTableContext ctx)
    {
        return (Max + Min) / 2f;
    }
}