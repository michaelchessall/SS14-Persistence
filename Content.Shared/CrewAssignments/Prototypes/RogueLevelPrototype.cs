using Robust.Shared.Prototypes;

namespace Content.Shared.CrewAssignments.Prototypes;

/// <summary>
/// This is a set of prototypes for rogue levels
/// that must be purchased in order, each level grants
/// various rewards
/// </summary>
[Prototype]
public sealed partial class RogueLevelPrototype : IPrototype
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
    /// XP Required To Reach This Level
    /// </summary>
    [DataField]
    public int Cost = 0;

    /// <summary>
    /// What RogueLevel is the next to level into
    /// </summary>
    [DataField]
    public ProtoId<RogueLevelPrototype>? Next = null;

    /// <summary>
    /// What level of items are available for purchase
    /// </summary>
    [DataField]
    public int ItemLevel = 0;


}
