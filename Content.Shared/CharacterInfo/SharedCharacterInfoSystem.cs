using Content.Shared.Objectives;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestCharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public RequestCharacterInfoEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly string JobTitle;
    public readonly string? Faction;
    public readonly string BankBal;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;
    public readonly string? DetailExaminable;

    public CharacterInfoEvent(NetEntity netEntity, string jobTitle, string? faction, string bankBal, Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing, string? detailExaminable)
    {
        NetEntity = netEntity;
        JobTitle = jobTitle;
        Faction = faction;
        BankBal = bankBal;
        Objectives = objectives;
        Briefing = briefing;
        DetailExaminable = detailExaminable;
    }
}

[Serializable, NetSerializable]
public sealed class UpdateDetailExaminableEvent : EntityEventArgs
{
    public readonly string Content;

    public UpdateDetailExaminableEvent(string content)
    {
        Content = content;
    }
}
