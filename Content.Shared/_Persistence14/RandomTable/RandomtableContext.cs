using System.Diagnostics.CodeAnalysis;
using Content.Shared._Persistence14.RandomTable.State;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Persistence14.RandomTable;

public sealed partial class RandomTableContext
{
    public required IRobustRandom Random { get; init; }
    public required IPrototypeManager PrototypeManager { get; init; }
    public required RandomTableSystem RandomTableSystem { get; init; }
    public required IEntityManager EntityManager { get; init; }
    public RandomTableStateComponent? State { get; init; } = null;

    /// <summary>
    /// Calls <see cref="RandomTableStateComponent.EnsureContext(RandomTableContextElement, string[])"/> on the State component of the context, provided it exists. 
    /// </summary>
    public bool EnsureContext(RandomTableContextElement element, params string[] dataKeys)
    {
        if (State is not null)
            return State.EnsureContext(element, dataKeys);
        return false;
    }
}

