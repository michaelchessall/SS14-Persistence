using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Resonance.Prototypes;

[Prototype]
public sealed partial class ResonanceProductPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public string Name = "";
    [DataField]
    public string Description = "";
    [DataField]
    public bool Unique = true;
    [DataField]
    public int Price = 0;
    [DataField]
    public bool Shared = false;
    [DataField]
    public ResonanceProductType ProductType = ResonanceProductType.MainWorldBoundary;
    [DataField]
    public int RadiusExpansion = 0;
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
    public ProtoId<ResonanceProductPrototype>? Requires = null;

}

public enum ResonanceProductType : byte
{
    MainWorldBoundary
}
