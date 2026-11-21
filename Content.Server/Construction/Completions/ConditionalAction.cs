using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ConditionalAction : IGraphAction
    {
        [DataField("condition", required: true)] public IGraphCondition? Condition { get; private set; }

        [DataField("action", required: true)] public IGraphAction? Action { get; private set; }

        [DataField("else")] public IGraphAction? Else { get; private set; }

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (Condition == null || Action == null)
                return;

            if (Condition.Condition(uid, userUid, entityManager))
                Action.PerformAction(uid, userUid, entityManager);
            else
                Else?.PerformAction(uid, userUid, entityManager);
        }
    }
}
