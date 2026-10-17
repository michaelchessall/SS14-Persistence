using System.Linq;
using Content.Shared._Persistence14.Botany;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Persistence14.Random;

public static class RobustRandomExtensions
{
    // The four functions below were copied and modified from SharedRandomExtensions
    public static (string reagent, ProtoId<PlantChemQuantityPrototype> quantity) Pick(this WeightedRandomFillPlantChemicalPrototype prototype, IRobustRandom? random = null)
    {
        var randomFill = prototype.PickRandomFill(random);

        IoCManager.Resolve(ref random);

        var sum = randomFill.Reagents.Count;
        var accumulated = 0f;

        var rand = random.NextFloat() * sum;

        foreach (var reagent in randomFill.Reagents)
        {
            accumulated += 1f;

            if (accumulated >= rand)
            {
                return (reagent, randomFill.Quantity);
            }
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted pick for {prototype.ID}!");
    }

    public static RandomFillPlantChemical PickRandomFill(this WeightedRandomFillPlantChemicalPrototype prototype, IRobustRandom? random = null)
    {
        IoCManager.Resolve(ref random);

        var fills = prototype.Fills;
        Dictionary<RandomFillPlantChemical, float> picks = new();

        foreach (var fill in fills)
        {
            picks[fill] = fill.Weight;
        }

        var sum = picks.Values.Sum();
        var accumulated = 0f;

        var rand = random.NextFloat() * sum;

        foreach (var (randSolution, weight) in picks)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return randSolution;
            }
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted pick for {prototype.ID}!");
    }

    public static (Gas reagent, ProtoId<PlantGasQuantityPrototype> quantity) Pick(this WeightedRandomFillPlantGasPrototype prototype, IRobustRandom? random = null)
    {
        var randomFill = prototype.PickRandomFill(random);

        IoCManager.Resolve(ref random);

        var sum = randomFill.Gasses.Count;
        var accumulated = 0f;

        var rand = random.NextFloat() * sum;

        foreach (var reagent in randomFill.Gasses)
        {
            accumulated += 1f;

            if (accumulated >= rand)
            {
                return (reagent, randomFill.Quantity);
            }
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted pick for {prototype.ID}!");
    }

    public static RandomFillPlantGas PickRandomFill(this WeightedRandomFillPlantGasPrototype prototype, IRobustRandom? random = null)
    {
        IoCManager.Resolve(ref random);

        var fills = prototype.Fills;
        Dictionary<RandomFillPlantGas, float> picks = new();

        foreach (var fill in fills)
        {
            picks[fill] = fill.Weight;
        }

        var sum = picks.Values.Sum();
        var accumulated = 0f;

        var rand = random.NextFloat() * sum;

        foreach (var (randSolution, weight) in picks)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return randSolution;
            }
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted pick for {prototype.ID}!");
    }

    public static IEnumerable<T> PickWeighted<T>(this IRobustRandom random, IEnumerable<T> values, Func<T, float> getWeight, int count = 1)
    {
        if (count <= 0)
            yield break;

        var sumWeight = 0f;
        foreach (var v in values)
            sumWeight += getWeight(v);

        if (sumWeight <= 0f)
            yield break;

        for (int i = 0; i < count; i++)
        {
            var weight = random.NextFloat(0f, sumWeight);
            var inc = 0f;
            foreach (var v in values)
            {
                inc += getWeight(v);
                if (inc > weight)
                {
                    yield return v;
                    break;
                }
            }
        }
    }

    public static IEnumerable<TKey> PickWeighted<TKey>(this IRobustRandom random, Dictionary<TKey, float> values, int count = 1) where TKey : notnull
        => PickWeighted(random, values.Keys.ToList(), key => values[key], count);

    public static IEnumerable<T> PickAndTakeWeighted<T>(this IRobustRandom random, List<T> values, Func<T, float> getWeight, int count = 1)
    {
        if (count <= 0)
            yield break;

        var sumWeight = 0f;
        foreach (var v in values)
            sumWeight += getWeight(v);

        if (sumWeight <= 0f)
            yield break;

        for (int i = 0; i < count; i++)
        {
            if (values.Count <= 0)
                yield break;

            var weight = random.NextFloat(0f, sumWeight);
            var inc = 0f;
            foreach (var v in values.ToArray())
            {
                var vWeight = getWeight(v);
                inc += vWeight;
                if (inc > weight)
                {
                    values.Remove(v);
                    sumWeight -= vWeight;
                    yield return v;
                    if (sumWeight <= 0f)
                        yield break;
                    break;
                }
            }
        }
    }
    public static IEnumerable<TKey> PickAndTakeWeighted<TKey>(this IRobustRandom random, Dictionary<TKey, float> values, int count = 1) where TKey : notnull
    {
        if (count <= 0)
            yield break;

        var sumWeight = 0f;
        foreach (var (_, weight) in values)
            sumWeight += weight;

        if (sumWeight <= 0f)
            yield break;

        for (int i = 0; i < count; i++)
        {
            if (values.Count <= 0)
                yield break;

            var weight = random.NextFloat(0f, sumWeight);
            var inc = 0f;
            foreach (var (key, vWeight) in values.ToArray())
            {
                inc += vWeight;
                if (inc > weight)
                {
                    values.Remove(key);
                    sumWeight -= vWeight;
                    yield return key;
                    if (sumWeight <= 0f)
                        yield break;
                    break;
                }
            }
        }
    }
}
