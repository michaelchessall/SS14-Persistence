using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Picks a uniformly random point within a radius of an anchor coordinate read from the
/// blackboard, and writes it to the target key (default "TargetCoordinates", matching
/// <see cref="MoveToOperator"/>). Unlike <see cref="PickAccessibleOperator"/>, which wanders near
/// the NPC's current position, this samples around a fixed anchor - for guardian-style mobs that
/// should roam a specific point rather than drift. Doesn't verify reachability; relies on
/// MoveToOperator to fail gracefully and retry next plan.
/// </summary>
public sealed partial class PickPointNearKeyOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Blackboard key holding the EntityCoordinates to sample around.
    /// </summary>
    [DataField("anchorKey", required: true)]
    public string AnchorKey = string.Empty;

    /// <summary>
    /// Blackboard key holding the float radius (in tiles) to sample within.
    /// </summary>
    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    /// <summary>
    /// Where the resulting coordinates get written.
    /// </summary>
    [DataField("targetCoordinates")]
    public string TargetCoordinates = "TargetCoordinates";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityCoordinates>(AnchorKey, out var anchor, _entManager))
            return (false, null);

        var range = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
        if (range <= 0f)
            return (false, null);

        var angle = _random.NextFloat(0f, MathF.Tau);
        var distance = MathF.Sqrt(_random.NextFloat(0f, 1f)) * range; // sqrt for uniform area density
        var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

        var target = anchor.Offset(offset);

        return (true, new Dictionary<string, object>
        {
            { TargetCoordinates, target },
        });
    }
}
