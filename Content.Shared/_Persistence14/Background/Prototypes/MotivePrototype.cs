using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Background.Prototypes;

[Prototype]
public sealed partial class MotivePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = "";

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public List<ProtoId<BackgroundEffectPrototype>> Effects = new();

}
