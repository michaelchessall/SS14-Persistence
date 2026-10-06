using Content.Shared._Persistence14.Rumors.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Rumors.Components;

[RegisterComponent]
public sealed partial class RumorGetterComponent : Component
{
    [DataField]
    public List<ActiveRumor> Rumors = new();
    [DataField]
    public TimeSpan NextRumor = TimeSpan.FromMinutes(60);
    [DataField]
    public TimeSpan RumorCooldownLength = TimeSpan.FromMinutes(60);
    [DataField]
    public Dictionary<ProtoId<MetaFactionPrototype>, Dictionary<ProtoId<MetaFactionLevelPrototype>, List<ActiveRumorReward>>> RumorRewards = new();

}
