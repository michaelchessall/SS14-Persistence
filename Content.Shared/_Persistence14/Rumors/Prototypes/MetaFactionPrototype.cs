using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Rumors.Prototypes;

[Prototype]
public sealed partial class MetaFactionPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public Dictionary<ProtoId<MetaFactionLevelPrototype>, int> Levels = new Dictionary<ProtoId<MetaFactionLevelPrototype>, int>();

    [DataField]
    public List<ProtoId<RumorPrototype>> Rumors = new List<ProtoId<RumorPrototype>>();

    [DataField]
    public Color Color = Color.DarkRed;
    [DataField]
    public string BaseTitle = "Stranger";
}
