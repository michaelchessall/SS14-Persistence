using Content.Shared.CrewAssignments.Components;
using Content.Shared.DoAfter;
using Content.Shared.Precursor;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using static Robust.Shared.Physics.DynamicTree;

namespace Content.Shared.CrewAssignments.Systems;

[Virtual]
public abstract partial class SharedJobNetSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public virtual void ReagentObjectiveComplete(JobNetComponent component, ProtoId<PrecursorObjectivePrototype> objective)
    {

    }

    public void JitterObjectiveTryComplete(JobNetComponent component)
    {
        foreach (var objective in component.PrecursorObjectives.ToList())
        {
            if (_proto.TryIndex(objective, out PrecursorObjectivePrototype? proto) && proto != null)
            {
                if (proto.TargetStatus == StatusEffectType.Jitter)
                {
                    ReagentObjectiveComplete(component, objective);
                }
            }
        }
    }
    public void ReagentObjectiveTryComplete(JobNetComponent component, Entity<StatusEffectComponent?> ent)
    {
        if (ent.Comp == null)
            return;
        foreach (var objective in component.PrecursorObjectives.ToList())
        {
            if (_proto.TryIndex(objective, out PrecursorObjectivePrototype? proto) && proto != null)
            {
                if (proto.TargetStatus == StatusEffectType.Drunk)
                {
                    if (Name(ent.Owner) == "drunk")
                    {
                        if (ent.Comp.EndEffectTime != null && (ent.Comp.EndEffectTime.Value - _timing.CurTime).TotalSeconds >= proto.RequiredAmount)
                        {
                            ReagentObjectiveComplete(component, objective);
                        }
                    }
                }
                if (proto.TargetStatus == StatusEffectType.Hallucinate)
                {
                    if (Name(ent.Owner) == "hallucinations")
                    {
                        if (ent.Comp.EndEffectTime != null && (ent.Comp.EndEffectTime.Value - _timing.CurTime).TotalSeconds >= proto.RequiredAmount)
                        {
                            ReagentObjectiveComplete(component, objective);
                        }
                    }
                }
            }
        }
    }
    [Serializable, NetSerializable]
    public sealed partial class PrecursorExtractorDoAfterEvent : DoAfterEvent
    {
        public PrecursorExtractorDoAfterEvent()
        {
        }

        public override DoAfterEvent Clone() => this;
    }
}
