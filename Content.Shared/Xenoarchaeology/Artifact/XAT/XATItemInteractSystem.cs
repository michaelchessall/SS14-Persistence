using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

public sealed partial class XATItemInteractSystem : BaseXATSystem<XATItemInteractComponent>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedDoAfterSystem _doafter = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private ILogManager _log = default!;

    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractUsing);
        XATSubscribeDirectEvent<XATItemInteractDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(Entity<XenoArtifactComponent> artifact, Entity<XATItemInteractComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (!_whitelist.IsWhitelistPass(node.Comp1.Whitelist, args.Used) || args.Handled || !CanModifyStack(node.Comp1, args.Used))
            return;

        if (node.Comp1.UseTime <= TimeSpan.Zero)
        {
            ModifyStack(node.Comp1, args.Used);
            Trigger(artifact, node);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            node.Comp1.UseTime,
            new XATItemInteractDoAfterEvent(GetNetEntity(node)),
            artifact,
            target: artifact,
            used: args.Used
        )
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doafter.TryStartDoAfter(doAfterArgs))
            return;
        args.Handled = true;
    }

    private void OnDoAfter(Entity<XenoArtifactComponent> artifact, Entity<XATItemInteractComponent, XenoArtifactNodeComponent> node, ref XATItemInteractDoAfterEvent args)
    {
        _log.GetSawmill("xat-item-interact").Info("Do After Event Received");
        if (args.Cancelled || args.Handled)
            return;
        _log.GetSawmill("xat-item-interact").Info("Valid cancel and handle status");
        if (GetEntity(args.Node) != node.Owner)
            return;

        _log.GetSawmill("xat-item-interact").Info("node entity valid and matching");
        if (args.Used is not { } used)
            return;

        _log.GetSawmill("xat-item-interact").Info("item valid");
        if (TerminatingOrDeleted(used))
            return;

        _log.GetSawmill("xat-item-interact").Info("item still exists");
        if (!_whitelist.IsWhitelistPass(node.Comp1.Whitelist, used))
            return;
        _log.GetSawmill("xat-item-interact").Info("item matches whitelist");

        ModifyStack(node.Comp1, used);
        Trigger(artifact, node);
        args.Handled = true;
    }

    private bool CanModifyStack(XATItemInteractComponent xatComponent, EntityUid item)
    {
        if (xatComponent.ReduceStackBy <= 0 || !CanModifyStack(xatComponent, item))
            return true;

        if (!TryComp<StackComponent>(item, out var stack))
            return false;

        return stack.Unlimited || stack.Count >= xatComponent.ReduceStackBy;
    }

    private void ModifyStack(XATItemInteractComponent xatComponent, EntityUid item)
    {
        if (xatComponent.ReduceStackBy <= 0) return;

        if (!TryComp<StackComponent>(item, out var stack) || stack.Unlimited) return;

        if (!CanModifyStack(xatComponent, item)) return;

        _stack.TryUse((item, stack), xatComponent.ReduceStackBy);
    }
}