using Content.Shared.Objectives;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Prototypes;
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
    public readonly ProtoId<JobPrototype>? Job;
    public readonly string? Faction;
    public readonly string BankBal;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;
    public readonly string? DetailExaminable;
    public readonly List<ProtoId<TraitPrototype>> Traits;

    public CharacterInfoEvent(NetEntity netEntity, string? faction, string bankBal, Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing, string? detailExaminable, ProtoId<JobPrototype>? job, List<ProtoId<TraitPrototype>> traits)
    {
        NetEntity = netEntity;
        Objectives = objectives;
        Briefing = briefing;
        DetailExaminable = detailExaminable;
        Faction = faction;
        BankBal = bankBal;
        Job = job;
        Traits = traits;
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
