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
    [DataField]
    public List<ProtoId<RumorPrototype>> Rumors = new List<ProtoId<RumorPrototype>>();
    [DataField]
    public List<ProtoId<RumorRewardPrototype>> RumorRewards = new List<ProtoId<RumorRewardPrototype>>();
    [DataField]
    public int RewardsToOffer = 0;

}
