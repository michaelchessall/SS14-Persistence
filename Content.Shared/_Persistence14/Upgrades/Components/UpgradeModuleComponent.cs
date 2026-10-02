using Content.Shared._Persistence14.Upgrades.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Upgrades.Components;

[RegisterComponent]
public sealed partial class UpgradeModuleComponent : Component
{
    [DataField]
    public ProtoId<UpgradeModuleTypePrototype> TargetModule { get; set; } = string.Empty;
}
