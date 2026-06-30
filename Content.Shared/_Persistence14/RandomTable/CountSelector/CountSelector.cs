using Content.Shared._Persistence14.RandomTable.ValueDefinition;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Persistence14.RandomTable.Count;

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class CountSelector
{
    public abstract int Get(RandomTableContext ctx);
    /// <summary>
    /// Probability of occurrence
    /// </summary>
    /// <returns>The probability the number of occurances is at least <see cref="threshold"/>.</returns>
    public abstract float Probability(RandomTableContext ctx, int threshold = 1);

    /// <summary>
    /// Average number of occurrences
    /// </summary>
    /// <returns>The average amount of occurrences</returns>
    public abstract float Average(RandomTableContext ctx);
}