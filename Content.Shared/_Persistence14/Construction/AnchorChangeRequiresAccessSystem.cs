using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Construction.Components;

namespace Content.Shared._Persistence14.Construction;

public sealed partial class AnchorChangeRequiresAccessSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;

    [SubscribeLocalEvent]
    private void OnAnchorAttempt(Entity<AnchorChangeRequiresAccessComponent> ent, ref AnchorAttemptEvent args)
    {
        if (!ent.Comp.Flags.HasFlag(AnchorableFlags.Anchorable) ||
            !TryComp<AccessReaderComponent>(ent.Owner, out var accessComp))
            return;

        if (!_access.IsAllowed(args.User, ent.Owner, accessComp))
        {
            args.Cancel();
            args.FailMessage = Loc.TryGetString(ent.Comp.FailureMessage, out var msg) ? msg : ent.Comp.FailureMessage;
        }
    }

    [SubscribeLocalEvent]
    private void OnUnanchorAttempt(Entity<AnchorChangeRequiresAccessComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (!ent.Comp.Flags.HasFlag(AnchorableFlags.Unanchorable) ||
            !TryComp<AccessReaderComponent>(ent.Owner, out var accessComp))
            return;

        if (!_access.IsAllowed(args.User, ent.Owner, accessComp))
        {
            args.Cancel();
            args.FailMessage = Loc.TryGetString(ent.Comp.FailureMessage, out var msg) ? msg : ent.Comp.FailureMessage;
        }
    }
}