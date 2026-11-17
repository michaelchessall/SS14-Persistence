using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Construction;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Construction.Transform;

[DataDefinition]
public sealed partial class CopyAccessReader : IGraphTransform
{
    public void Transform(EntityUid oldUid, EntityUid newUid, EntityUid? userUid, GraphTransformArgs args)
    {
        if (!args.EntityManager.TryGetComponent<AccessReaderComponent>(oldUid, out var oldAccess))
            return;

        var target = args.EntityManager.EnsureComponent<AccessReaderComponent>(newUid);

        target.Enabled = oldAccess.Enabled;
        target.DenyTags = [.. oldAccess.DenyTags];
        target.AccessLists = CopyLists(oldAccess.AccessLists);
        target.AccessListsOriginal = oldAccess.AccessListsOriginal is null
            ? null
            : CopyLists(oldAccess.AccessListsOriginal);
        target.AccessKeys = [.. oldAccess.AccessKeys];
        target.AccessNames = [.. oldAccess.AccessNames];
        target.ContainerAccessProvider = oldAccess.ContainerAccessProvider;
        target.PersonalAccessMode = oldAccess.PersonalAccessMode;
        target.PersonalAccessNames = [.. oldAccess.PersonalAccessNames];

        // Preserve reader behavior and settings too.
        target.AccessLogLimit = oldAccess.AccessLogLimit;
        target.LoggingDisabled = oldAccess.LoggingDisabled;
        target.BreakOnAccessBreaker = oldAccess.BreakOnAccessBreaker;
        target.ExaminationText = oldAccess.ExaminationText;
        target.BlockUnanchor = oldAccess.BlockUnanchor;

        args.EntityManager.Dirty(newUid, target);
    }

    private static List<HashSet<ProtoId<AccessLevelPrototype>>> CopyLists(
       IEnumerable<HashSet<ProtoId<AccessLevelPrototype>>> source)
    {
        return source
            .Select(accessGroup => new HashSet<ProtoId<AccessLevelPrototype>>(accessGroup))
            .ToList();
    }
}