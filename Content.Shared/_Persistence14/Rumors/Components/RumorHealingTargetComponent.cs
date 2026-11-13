using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Rumors.Components;

[RegisterComponent]
public sealed partial class RumorHealingTargetComponent : Component
{
    [DataField]
    public HashSet<ProtoId<DamageTypePrototype>> PossibleDamageTypes = new();
    [DataField]
    public int MinDamageAmount = 40;
    [DataField]
    public int MaxDamageAmount = 140;
    [DataField]
    public int MinDamageTypes = 2;
    [DataField]
    public int MaxDamageTypes = 4;

}
