using Content.Shared.CrewAccesses.Components;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.CrewAssignments.Prototypes;
using Content.Shared.Radio;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class StationModificationInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public string FactionTag;
    public NetEntity Station;
    public List<string> Owners;
    public Dictionary<string, CrewAccess> CrewAccess;
    public Dictionary<int, CrewAssignment> CrewAssignments;
    public int ImportTax;
    public int ExportTax;
    public int SalesTax;
    public ProtoId<FactionLevelPrototype> Level;
    public int AccountBalance;
    public Dictionary<ProtoId<RadioChannelPrototype>, FactionRadioData> RadioData;
    public bool JobNetEnabled;
    public bool TradeStationClaimed;

    public StationModificationInterfaceState(string name, string factionTag, NetEntity station, List<string> owners, Dictionary<string, CrewAccess> crewAccess, Dictionary<int, CrewAssignment> crewAssignments, int importTax, int exportTax, int salesTax, ProtoId<FactionLevelPrototype> level, int accountBalance, Dictionary<ProtoId<RadioChannelPrototype>, FactionRadioData> radioData, bool jobNetEnabled, bool tradeStationClaimed)
    {
        Name = name;
        FactionTag = factionTag;
        Station = station;
        Owners = owners;
        CrewAccess = crewAccess;
        CrewAssignments = crewAssignments;
        ImportTax = importTax;
        ExportTax = exportTax;
        SalesTax = salesTax;
        Level = level;
        AccountBalance = accountBalance;
        RadioData = radioData;
        JobNetEnabled = jobNetEnabled;
        TradeStationClaimed = tradeStationClaimed;
    }
}
