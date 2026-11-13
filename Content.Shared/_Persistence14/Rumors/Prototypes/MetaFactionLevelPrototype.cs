using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Rumors.Prototypes;

[Prototype]
public sealed partial class MetaFactionLevelPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// Rumors added to the pool for this particular rumor level, provided the use has the required reputation.
    /// Required reputation is determined byh the MetaFactionPrototype.
    /// </summary>
    [DataField]
    public List<ProtoId<RumorPrototype>> Rumors = new List<ProtoId<RumorPrototype>>();

    /// <summary>
    /// Potential rewards for this meta faction level in the rumor market.
    /// </summary>
    [DataField]
    public List<ProtoId<RumorRewardPrototype>> RumorRewards = new List<ProtoId<RumorRewardPrototype>>();

    /// <summary>
    /// The number of rumor rewards of this level to display in the rumor market at one time.
    /// </summary>
    [DataField]
    public int RewardsToOffer = 0;

}
