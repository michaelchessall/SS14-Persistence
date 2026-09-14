using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// Handles <see cref="XATFeedComponent"/>: feeding a whitelisted item to the artifact via a do-after,
/// consuming exactly one of that item when it completes.
/// </summary>
public sealed class XATFeedSystem : BaseXATSystem<XATFeedComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractUsing);
        XATSubscribeDirectEvent<XATFeedDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(Entity<XenoArtifactComponent> artifact, Entity<XATFeedComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (args.Handled || !Matches(node.Comp1, args.Used))
            return;

        if (node.Comp1.Delay <= TimeSpan.Zero)
        {
            ConsumeOne(args.Used);
            Trigger(artifact, node);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            node.Comp1.Delay,
            new XATFeedDoAfterEvent(GetNetEntity(node)),
            artifact,
            target: artifact,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            args.Handled = true;
    }

    private void OnDoAfter(Entity<XenoArtifactComponent> artifact, Entity<XATFeedComponent, XenoArtifactNodeComponent> node, ref XATFeedDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (GetEntity(args.Node) != node.Owner)
            return;

        if (args.Used is not { } used || TerminatingOrDeleted(used))
            return;

        if (!Matches(node.Comp1, used))
            return;

        ConsumeOne(used);
        Trigger(artifact, node);
        args.Handled = true;
    }

    /// <summary>
    /// True if the item satisfies the trigger: its prototype is listed in <see cref="XATFeedComponent.Foods"/>,
    /// or it matches the optional <see cref="XATFeedComponent.Whitelist"/>.
    /// </summary>
    private bool Matches(XATFeedComponent comp, EntityUid item)
    {
        if (comp.Foods.Count > 0 && MetaData(item).EntityPrototype?.ID is { } id && comp.Foods.Contains(id))
            return true;

        return comp.Whitelist != null && _whitelist.IsWhitelistPass(comp.Whitelist, item);
    }

    /// <summary>
    /// Consumes a single unit of the fed item: one is taken from a stack (deleting it only if that was
    /// the last), or a non-stacked entity is deleted outright. Never consumes a whole stack at once.
    /// </summary>
    private void ConsumeOne(EntityUid item)
    {
        if (TryComp<StackComponent>(item, out var stack))
            _stack.TryUse((item, stack), 1);
        else
            PredictedQueueDel(item);
    }
}
