using Content.Shared.Anomaly.Components;

namespace Content.Shared._Persistence14.RandomTable.Conditions;

public sealed partial class RTCAnomalySeverity : RandomTableCondition
{
    [DataField("min")]
    public float MinSeverityThreshold = 0f;

    [DataField("max")]
    public float MaxSeverityThreshold = 1f;

    protected override bool EvaluateImplementation(RandomTableSelector selector, RandomTableContext ctx)
    {
        if (!ctx.EnsureContext(State.RandomTableContextElement.Source) || !ctx.EntityManager.TryGetComponent<AnomalyComponent>(ctx.State?.Source, out var anomaly))
            return false; // If it doesn't have an anomaly component... it shouldn't evaluate true.

        return anomaly.Severity >= MinSeverityThreshold && anomaly.Severity <= MaxSeverityThreshold;
    }
}