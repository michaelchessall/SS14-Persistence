using Content.Shared._Persistence14.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires some chemical reagent.
/// </summary>
public sealed class XATReactiveSystem : BaseXATSystem<XATReactiveComponent>
{
    [Dependency] private ReagentWhitelistSystem _reagentWhitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<ReactionEntityEvent>(OnReaction);
    }

    private void OnReaction(Entity<XenoArtifactComponent> artifact, Entity<XATReactiveComponent, XenoArtifactNodeComponent> node, ref ReactionEntityEvent args)
    {
        LogManager.GetSawmill("xat-reactive").Info($"Reactive Candidate: reagent={args.Reagent.ID}, node={ToPrettyString(node.Owner)}, method={args.Method}");

        var reactiveTriggerComponent = node.Comp1;
        if (!reactiveTriggerComponent.ReactionMethods.Contains(args.Method))
        {
            LogManager.GetSawmill("xat-reactive").Info($"Reactive failed: Invalid reaction method.");
            return;
        }

        if (args.ReagentQuantity.Quantity < reactiveTriggerComponent.MinQuantity)
        {
            LogManager.GetSawmill("xat-reactive").Info($"Reactive failed: Insufficient reagent, needed: {reactiveTriggerComponent.MinQuantity}, received: {args.ReagentQuantity.Quantity}");
            return;
        }
        var reagent = args.ReagentQuantity.Reagent.Prototype;

        if (_reagentWhitelist.WhitelistFail(reactiveTriggerComponent.Whitelist, reagent) ||
            _reagentWhitelist.BlacklistFail(reactiveTriggerComponent.Blacklist, reagent))
        {
            LogManager.GetSawmill("xat-reactive").Info($"Reactive failed: Whitelist failed.");
            return;
        }

        Trigger(artifact, node);
    }
}
