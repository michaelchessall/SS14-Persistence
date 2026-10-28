using Content.Shared._Persistence14.Upgrades.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.CrewAssignments.Prototypes;

/// <summary>
/// This is a set of prototypes for faction levels
/// that must be purchased in order, each level grants
/// various rewards
/// </summary>
[Prototype]
public sealed partial class FactionLevelPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A description for flava purposes.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// Cost to purchase this faction level.
    /// </summary>
    [DataField]
    public int Cost = 0;

    [DataField]
    public Dictionary<ProtoId<UpgradeModuleTypePrototype>, int> RequiredModules = new();

    /// <summary>
    /// How many tiles this faction is allowed to claim.
    /// </summary>
    [DataField]
    public int TileLimit = 0;

    /// <summary>
    /// Whether the faction can claim a tradestation
    /// </summary>
    [DataField]
    public bool TradestationClaim = false;

    /// <summary>
    /// What StationLevel is the next available to purchase
    /// </summary>
    [DataField]
    public ProtoId<FactionLevelPrototype>? Next = null;

}
