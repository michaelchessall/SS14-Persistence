using Content.Shared._Persistence14.Rumors.Prototypes;
using Robust.Shared.Audio;
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
    public TimeSpan NextReward = TimeSpan.FromMinutes(0);
    [DataField]
    public TimeSpan RewardCooldownLength = TimeSpan.FromMinutes(150);

    [DataField]
    public TimeSpan NextRumor = TimeSpan.FromMinutes(120);
    [DataField]
    public TimeSpan RumorCooldownLength = TimeSpan.FromMinutes(120);
    [DataField]
    public Dictionary<ProtoId<MetaFactionPrototype>, Dictionary<ProtoId<MetaFactionLevelPrototype>, List<ActiveRumorReward>>> RumorRewards = new();
    public SoundSpecifier NewRumorSound = new SoundPathSpecifier("/Audio/Effects/RingtoneNotes/b.ogg");
    public SoundSpecifier NewRewardSound = new SoundPathSpecifier("/Audio/Effects/RingtoneNotes/f.ogg");
    public SoundSpecifier KachingSound = new SoundPathSpecifier("/Audio/Effects/kaching.ogg");
}
