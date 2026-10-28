using Content.Shared._Persistence14.Upgrades.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Upgrades.Components;

[RegisterComponent]
public sealed partial class UpgradeModuleComponent : Component
{
    [DataField]
    public ProtoId<UpgradeModuleTypePrototype> TargetModule { get; set; } = string.Empty;
    public SoundSpecifier ApplySound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");
}
