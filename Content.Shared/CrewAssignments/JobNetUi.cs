using Content.Shared._Persistence14.Rumors.Prototypes;
using Content.Shared.Cargo;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.CrewAssignments.Prototypes;
using Content.Shared.CrewAssignments.Systems;
using Content.Shared.CrewMetaRecords;
using Content.Shared.Precursor;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CrewAssignments;

[Serializable, NetSerializable]
public enum JobNetUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class JobNetUpdateState : BoundUserInterfaceState
{
    public Dictionary<int, string>? Stations { get; set; }
    public string? AssignmentName;
    public int? Wage;
    public int SelectedStation;
    public TimeSpan? RemainingMinutes;
    public List<WorldObjectivesEntry> CurrentObjectives;
    public List<WorldObjectivesEntry> CompletedObjectives;
    public List<CodexEntry> CodexEntries;
    public ProtoId<NetworkLevelPrototype> Level;
    public int Balance;
    public bool SpendAuth;
    public int Spent;
    public int Spendable;
    public string SectorStatus;
    public Dictionary<ProtoId<MetaFactionPrototype>, int> MetaFactionReputations = new();
    public List<ActiveRumor> Rumors = new();
    public TimeSpan? RumorCooldown;
    public int RumorTax;
    public Dictionary<string, DirectMessageConversation>? DirectMessages;


    public JobNetUpdateState(Dictionary<int, string>? stations, string? assignmentName, int? wage, int selectedStation, TimeSpan? remainingMinutes, List<WorldObjectivesEntry> currentObjectives, List<WorldObjectivesEntry> completedObjectives, List<CodexEntry> codexEntries, ProtoId<NetworkLevelPrototype> level, int balance, bool spendAuth, int spent, int spendable, string sectorStatus, Dictionary<ProtoId<MetaFactionPrototype>, int> metaFactionReputations, List<ActiveRumor> rumors, TimeSpan? rumorCooldown, int rumorTax, Dictionary<string, DirectMessageConversation>? directMessages)
    {
        Stations = stations;
        AssignmentName = assignmentName;
        Wage = wage;
        SelectedStation = selectedStation;
        RemainingMinutes = remainingMinutes;
        CurrentObjectives = currentObjectives;
        CompletedObjectives = completedObjectives;
        CodexEntries = codexEntries;
        Level = level;
        Balance = balance;
        SpendAuth = spendAuth;
        Spent = spent;
        Spendable = spendable;
        SectorStatus = sectorStatus;
        MetaFactionReputations = metaFactionReputations;
        Rumors = rumors;
        RumorCooldown = rumorCooldown;
        RumorTax = rumorTax;
        DirectMessages = directMessages;
    }
}

[Serializable, NetSerializable]
public sealed class JobNetRequestUpdateInterfaceMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class JobNetSelectMessage : BoundUserInterfaceMessage
{
    public int ID;
    public JobNetSelectMessage(int id)
    {
        ID = id;
    }
}

[Serializable, NetSerializable]
public sealed class JobNetCancelRumorMessage : BoundUserInterfaceMessage
{
    public int ID;
    public JobNetCancelRumorMessage(int id)
    {
        ID = id;
    }
}

[Serializable, NetSerializable]
public sealed class JobNetTransferRumorMessage : BoundUserInterfaceMessage
{
    public int ID;
    public string Target;
    public JobNetTransferRumorMessage(int id, string target)
    {
        ID = id;
        Target = target;
    }
}


[Serializable, NetSerializable]
public sealed class JobNetPurchaseMessage : BoundUserInterfaceMessage
{
    public JobNetPurchaseMessage()
    {
    }
}


[Serializable, NetSerializable]
public sealed class JobNetSelectRogueNetMessage : BoundUserInterfaceMessage
{
    public RogueNetworkType Net;
    public JobNetSelectRogueNetMessage(RogueNetworkType net)
    {
        Net = net;
    }
}


[Serializable, NetSerializable]
public sealed class JobNetDealerLabelMessage : BoundUserInterfaceMessage
{
    public string ID;
    public JobNetDealerLabelMessage(string id)
    {
        ID = id;
    }
}


[Serializable, NetSerializable]
public sealed class JobNetPurchasePrecursorMessage : BoundUserInterfaceMessage
{
    public string ID;
    public JobNetPurchasePrecursorMessage(string id)
    {
        ID = id;
    }
}



[Serializable, NetSerializable]
public sealed class JobNetSubmitHuntMessage : BoundUserInterfaceMessage
{
    public string ID;
    public JobNetSubmitHuntMessage(string id)
    {
        ID = id;
    }
}



[Serializable, NetSerializable]
public sealed class JobNetSubmitHuntedMessage : BoundUserInterfaceMessage
{
    public string ID;
    public JobNetSubmitHuntedMessage(string id)
    {
        ID = id;
    }
}

