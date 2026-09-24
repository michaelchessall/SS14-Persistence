using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Background.Prototypes;

[Prototype]
public sealed partial class BackgroundEffectPrototype : IPrototype, IEquipmentLoadout
{
    [IdDataField]
    public string ID { get; private set; } = "";

    [DataField]
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc />
    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    public List<EntProtoId> Inhand { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    public Dictionary<string, List<EntProtoId>> Storage { get; set; } = new();

    [DataField]
    public List<ProtoId<TraitPrototype>> Traits { get; set; } = new();

}
