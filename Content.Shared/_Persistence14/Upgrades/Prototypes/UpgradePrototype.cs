using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Upgrades.Prototypes;

[Prototype]
public sealed partial class UpgradePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; set; } = string.Empty;
    [DataField]
    public string Description { get; set; } = string.Empty;
    [DataField]
    public int MaxApplications { get; set; } = 1;
    [DataField]
    public ProtoId<UpgradeModuleTypePrototype> TargetModule { get; set; } = string.Empty;
    [DataField]
    public UpgradeType UpgradeType { get; set; } = UpgradeType.Storage;
    [DataField]
    public List<int> UpgradeValues { get; set; } = new List<int>();

}

public enum UpgradeType
{
    Storage,
    RadarRange,
    ApcMaxLoad,
    EnttiyStorageCapacity,
    BatteryCapacity,
    TelecomServerRange
}
