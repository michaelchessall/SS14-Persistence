
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;


namespace Content.Shared.Precursor;


[Serializable, NetSerializable]
public enum StatusEffectType : byte
{
    Drunk,
    Hallucinate,
    Jitter
}


[Prototype]
public sealed partial class PrecursorObjectivePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public int RequiredAmount { get; set; } = 5;

    [DataField]
    public int Reward { get; set; } = 100;

    [DataField]
    public StatusEffectType TargetStatus { get; set; } = StatusEffectType.Hallucinate;

    [DataField]
    public string Name { get; set; } = string.Empty;

}
