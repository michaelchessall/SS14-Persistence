using Content.Server._NF.Bank;
using Content.Server.CrewAssignments.Systems;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared.CCVar;
using Content.Shared.CharacterInfo;
using Content.Shared.DetailExaminable;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;
using Content.Shared.Traits;
using Content.Server.CrewRecords.Systems;

namespace Content.Server.CharacterInfo;

public sealed partial class CharacterInfoSystem : EntitySystem
{
    [Dependency] private JobSystem _jobs = default!;
    [Dependency] private MindSystem _minds = default!;
    [Dependency] private RoleSystem _roles = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;
    [Dependency] private BankSystem _bank = default!;
    [Dependency] private JobNetSystem _jobNet = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private CrewMetaRecordsSystem _crewMeta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestCharacterInfoEvent>(OnRequestCharacterInfoEvent);
        SubscribeNetworkEvent<UpdateDetailExaminableEvent>(OnUpdateDetailExaminableEvent);
    }

    private void OnRequestCharacterInfoEvent(RequestCharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue
            || args.SenderSession.AttachedEntity != GetEntity(msg.NetEntity))
            return;

        var entity = args.SenderSession.AttachedEntity.Value;

        var objectives = new Dictionary<string, List<ObjectiveInfo>>();

        var (jobTitle, faction) = _jobNet.GetJobNetStrings(entity); // Persistence: Job & faction names from implant
        if (jobTitle == null)
            jobTitle = Loc.GetString("character-info-off-duty");

        _bank.TryGetBalance(entity, out var bankBal);

        string? briefing = null;
        ProtoId<JobPrototype>? job = null;
        if (_minds.TryGetMind(entity, out var mindId, out var mind))
        {
            // Get objectives
            foreach (var objective in mind.Objectives)
            {
                var info = _objectives.GetInfo(objective, mindId, mind);
                if (info == null)
                    continue;

                if (!ProtoMan.TryIndex(Comp<ObjectiveComponent>(objective).Issuer, out var issuerProto))
                {
                    Log.Error($"Found incorrect objective issuer {issuerProto} when generating character info for objective {MetaData(objective).EntityPrototype}.");
                    continue;
                }

                // group objectives by their issuer
                var issuer = issuerProto.LocalizedName;
                if (!objectives.ContainsKey(issuer))
                    objectives[issuer] = new List<ObjectiveInfo>();
                objectives[issuer].Add(info.Value);
            }

            if (_jobs.MindTryGetJob(mindId, out var j))
                job = j;

            // Get briefing
            briefing = _roles.MindGetBriefing(mindId);
        }

        var detailExaminable = EnsureComp<DetailExaminableComponent>(entity, out var detail) ? detail.Content : Loc.GetString("flavor-text-placeholder");
        List<ProtoId<TraitPrototype>> traits = new();
        if(_crewMeta.MetaRecords != null)
        {
            if(_crewMeta.MetaRecords.TryGetRecord(Name(entity), out var record) && record != null)
            {
                traits = record.Traits;
            }

        }
        RaiseNetworkEvent(new CharacterInfoEvent(
            netEntity: GetNetEntity(entity),
            job: jobTitle,
            faction: faction,
            bankBal: "$" + bankBal.ToString(),
            objectives: objectives,
            briefing: briefing,
            detailExaminable: detailExaminable,
            traits: traits),
            args.SenderSession
        );

        Dirty(entity, detail);
    }

    private void OnUpdateDetailExaminableEvent(UpdateDetailExaminableEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } entity)
            return;

        string newContent = "";
        var maxFlavorTextLength = _cfg.GetCVar(CCVars.MaxFlavorTextLength);
        if (msg.Content.Length > maxFlavorTextLength)
        {
            newContent = FormattedMessage.RemoveMarkupOrThrow(msg.Content)[..maxFlavorTextLength];
        }
        else
        {
            newContent = FormattedMessage.RemoveMarkupOrThrow(msg.Content);
        }

        var detail = EnsureComp<DetailExaminableComponent>(entity);
        detail.Content = newContent;
        Dirty(entity, detail);
    }

    //     var maxFlavorTextLength = configManager.GetCVar(CCVars.MaxFlavorTextLength);
    //         if (FlavorText.Length > maxFlavorTextLength)
    //     {
    //         flavortext = FormattedMessage.RemoveMarkupOrThrow(FlavorText)[..maxFlavorTextLength];
    //     }
    // else
    // {
    //     flavortext = FormattedMessage.RemoveMarkupOrThrow(FlavorText);
    // }
}
