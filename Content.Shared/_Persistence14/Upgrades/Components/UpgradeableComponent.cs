using Content.Shared._Persistence14.Upgrades.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Persistence14.Upgrades.Components;

[RegisterComponent]
public sealed partial class UpgradeableComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<UpgradePrototype>, int> Upgrades = new();
    [DataField]
    public string Verb = "Upgrade";

    [DataField("verbImage")]
    [ViewVariables(VVAccess.ReadOnly)]
    public SpriteSpecifier? VerbImage = new SpriteSpecifier.Texture(new("/Textures/Interface/hammer.svg.192dpi.png"));

}
