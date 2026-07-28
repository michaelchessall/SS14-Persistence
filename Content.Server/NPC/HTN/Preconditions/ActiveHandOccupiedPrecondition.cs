namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Logical inverse of the stock ActiveHandFreePrecondition: met only when the active hand is
/// actually occupied. Needed to gate a "clear hand, then pick up a gun" branch - without it,
/// ClearActiveHandOperator trivially no-ops when the hand is already free, so a branch built
/// around it (having nothing else to possibly fail) would always win branch priority over the
/// real walk-over-and-pick-up branch on every replan, and the NPC would never move toward a gun
/// it could reach.
/// </summary>
public sealed partial class ActiveHandOccupiedPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return !(blackboard.TryGetValue<bool>(NPCBlackboard.ActiveHandFree, out var handFree, _entManager) && handFree);
    }
}
