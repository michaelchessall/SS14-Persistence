using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Prototypes;


[Prototype]
public sealed partial class InfrastructureLevelPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public int RequiredXP = 500;

    [DataField]
    public int DemotionXP = 300;

    [DataField]
    public string Name = "";

    [DataField]
    public List<ProtoId<CargoMarketPrototype>> Markets = new()
    {
        "market"
    };

    [DataField]
    public Dictionary<ProtoId<CargoBountyGroupPrototype>, int> Groups = new() { { "StationBounty", 4 }, { "ServiceBounty", 2 } };

    [DataField]
    public int Income = -5000;

    [DataField]
    public int Level = 1;

}
