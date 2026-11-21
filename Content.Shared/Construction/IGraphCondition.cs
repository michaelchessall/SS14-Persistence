using Content.Shared.Examine;

namespace Content.Shared.Construction
{
    [ImplicitDataDefinitionForInheritors]
    public partial interface IGraphCondition
    {
        bool Condition(EntityUid uid, EntityUid? user, IEntityManager entityManager);
        bool DoExamine(ExaminedEvent args);
        IEnumerable<ConstructionGuideEntry> GenerateGuideEntry();
    }
}
