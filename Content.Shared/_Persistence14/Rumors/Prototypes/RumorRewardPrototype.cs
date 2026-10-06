using Content.Shared.Cargo;
using Content.Shared.Nutrition;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Rumors.Prototypes;

[Prototype]
public sealed partial class RumorRewardPrototype : IPrototype
{

    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Texture path used in the GUI.
    /// </summary>
    [DataField]
    public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

    /// <summary>
    ///     The entity prototype ID of the product.
    /// </summary>
    [DataField]
    public EntProtoId Product { get; private set; } = string.Empty;

    [DataField]
    public EntProtoId Container { get; private set; } = "CrateSecure";
    [DataField]
    public string ContainerId { get; private set; } = "entity_storage";
    [DataField]
    public string Name { get; set; } = string.Empty;
    [DataField]
    public string Description { get; set; } = string.Empty;
    [DataField]
    public int Price { get; set; } = 0;
}


[DataDefinition]
[Serializable]
[Virtual]
public partial class ActiveRumorReward
{
    [DataField]
    public ProtoId<RumorRewardPrototype> Reward = string.Empty;
    [DataField]
    public int TradeStationUID = 0;
    [DataField]
    public string TradeStationName = "";
    [DataField]
    public bool Purchased = false;
}
