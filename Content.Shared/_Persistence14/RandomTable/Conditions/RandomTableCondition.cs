using JetBrains.Annotations;

namespace Content.Shared._Persistence14.RandomTable;

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class RandomTableCondition
{
    /// <summary>
    /// If true, the condition evaluates to the opposite of the value.
    /// </summary>
    [DataField]
    public bool Invert = false;

    /// <summary>
    /// Evaluates the status of the condition.
    /// </summary>
    public bool Evaluate(RandomTableSelector selector, RandomTableContext ctx) => Invert ^ EvaluateImplementation(selector, ctx);

    /// <summary>
    /// Child specific implementation of Evaluate used when checking the condition of the child.
    /// </summary>
    protected abstract bool EvaluateImplementation(RandomTableSelector selector, RandomTableContext ctx);
}

/// <summary>
/// The evaluation criteria for lists of conditions <br/><br/>
/// <b>Any</b> - True if at least one of the conditions evaluates to true<br/>
/// <b>All</b> - True if and only if non of the conditions evaluate to false<br/>
/// <b>None</b> - True if and only if non of the conditions evaluate to true
/// </summary>
public enum RandomTableConditionMode
{
    Any,
    All,
    None,
}