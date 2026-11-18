using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Construction;
using Content.Shared.Examine;

namespace Content.Server._Persistence14.Construction.Conditions;

[DataDefinition]
public sealed partial class UserAccess : IGraphCondition
{
    public bool Condition(EntityUid uid, EntityUid? user, IEntityManager entityManager)
    {
        var accessReader = entityManager.System<AccessReaderSystem>();

        if (!entityManager.TryGetComponent<AccessReaderComponent>(uid, out var accessComp))
            return true; // If theres no access reader, is open access.

        if (user is not { })
            return false; // Nothing shouldn't have access...

        return accessReader.IsAllowed(user.Value, uid, accessComp);
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var uid = args.Examined;
        var user = args.Examiner;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var accessReader = entMan.System<AccessReaderSystem>();

        if (accessReader.IsAllowed(uid, user))
            return false; // No need to show if you already have access

        args.PushMarkup(Loc.GetString("construction-examine-condition-access"));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-step-condition-access"
        };
    }
}