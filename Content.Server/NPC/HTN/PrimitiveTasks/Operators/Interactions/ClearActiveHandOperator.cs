using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

/// <summary>
/// Self-contained equivalent of the stock ClearActiveHandCompound, but simplified: no-op if the
/// active hand is already free, otherwise unconditionally DROP whatever's in it - never swap it
/// into another hand. Intended for an NPC whose item preference is a straight discard (e.g. gun
/// beats melee beats unarmed); if a held item is dropped and later needed again, the NPC re-finds
/// it where it left it via the normal pickup branches, instead of having hoarded it in an
/// off-hand.
///
/// Inlined as a single primitive task instead of a nested compound because of how HTNPlanJob
/// backtracks: RestoreTolastDecomposedTask rolls back to the most recently decomposed compound on
/// the stack whenever a later primitive fails - not necessarily the branch containing the
/// failure - and clears the entire remaining task stack. A nested ClearActiveHandCompound
/// followed by more sibling tasks in the same parent branch (e.g. equip-a-carried-gun: clear
/// hand, find gun, equip) would get trapped retrying its own alternatives whenever a later
/// sibling failed, silently discarding those siblings and the parent's own fallback branches
/// (e.g. PickupGunCompound, MeleeCombatCompound) - leaving the NPC stuck doing nothing despite
/// valid targets/items nearby. A single flat primitive sidesteps this without touching the shared
/// compound or the planner itself.
/// </summary>
public sealed partial class ClearActiveHandOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var handsSystem = _entManager.System<HandsSystem>();

        if (!_entManager.TryGetComponent<HandsComponent>(owner, out var hands))
            return HTNOperatorStatus.Finished;

        var activeHand = handsSystem.GetActiveHand((owner, hands));

        if (activeHand == null || handsSystem.HandIsEmpty((owner, hands), activeHand))
            return HTNOperatorStatus.Finished;

        var dropped = handsSystem.TryDrop((owner, hands));

        return dropped ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;
    }
}
