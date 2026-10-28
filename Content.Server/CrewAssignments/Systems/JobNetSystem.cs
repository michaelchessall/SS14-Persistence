using Content.Server._NF.Bank;
using Content.Server._Persistence14.Codex.Systems;
using Content.Server.Access.Systems;
using Content.Server.Audio;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Codewords;
using Content.Server.CrewManifest;
using Content.Server.CrewRecords.Systems;
using Content.Server.Database;
using Content.Server.DoAfter;
using Content.Server.Interaction;
using Content.Server.NameIdentifier;
using Content.Shared._Persistence14.Rumors.Components;
using Content.Shared._Persistence14.Rumors.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CrewAssignments;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.CrewAssignments.Prototypes;
using Content.Shared.CrewAssignments.Systems;
using Content.Shared.CrewRecords.Components;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Precursor;
using Content.Shared.Station.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using static Content.Shared.Access.Components.AccessOverriderComponent;
using static Content.Shared.Access.Systems.SharedAccessOverriderSystem;

namespace Content.Server.CrewAssignments.Systems;

/// <summary>
/// Manages general interactions with a store and different entities,
/// getting listings for stores, and interfacing with the store UI.
/// </summary>
public sealed partial class JobNetSystem : SharedJobNetSystem
{
    [Dependency] private BankSystem _bank = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private CrewManifestSystem _crewManifest = default!;
    [Dependency] private IdCardSystem _card = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private RumorsSystem _rumors = default!;
    [Dependency] private CodexSystem _codex = default!;
    [Dependency] private ServerGlobalSoundSystem _globalsound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JobNetComponent, ActivatableUIOpenAttemptEvent>(OnJobNetOpenAttempt);
        SubscribeLocalEvent<JobNetComponent, BeforeActivatableUIOpenEvent>(BeforeActivatableUiOpen);

        SubscribeLocalEvent<JobNetComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<JobNetComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<JobNetComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<JobNetComponent, ImplantImplantedEvent>(OnJobNetImplantImplanted);
        SubscribeLocalEvent<JobNetComponent, OpenJobNetImplantEvent>(OnImplantActivate);
        SubscribeLocalEvent<JobNetComponent, JobNetSelectMessage>(OnSelect);
        SubscribeLocalEvent<JobNetComponent, JobNetPurchaseMessage>(OnPurchase);
        SubscribeLocalEvent<JobNetComponent, JobNetCancelRumorMessage>(OnCancelRumor);
        SubscribeLocalEvent<JobNetComponent, JobNetTransferRumorMessage>(OnTransferRumor);
        SubscribeLocalEvent<JobNetComponent, JobNetRumorRewardPurchaseMessage>(OnRumorRewardPurchase);
        SubscribeLocalEvent<JobNetComponent, JobNetInsightPurchaseMessage>(OnInsightPurchase);
        SubscribeLocalEvent<PrecursorExtractorComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<PrecursorExtractorComponent, PrecursorExtractorDoAfterEvent>(OnDoAfter);



        InitializeUi();
    }

    private void OnInsightPurchase(Entity<JobNetComponent> ent, ref JobNetInsightPurchaseMessage args)
    {
        EntityUid? player = null;
        var comp = Transform(ent);
        player = comp.ParentUid;
        if (player == null) return;
        _codex.OnInsightPurchase(player.Value, args);
        UpdateUserInterface(args.Actor, ent);
    }

    private void OnRumorRewardPurchase(Entity<JobNetComponent> ent, ref JobNetRumorRewardPurchaseMessage args)
    {
        if (!TryComp<RumorGetterComponent>(ent, out var getter) || getter == null) return;
        _rumors.PurchaseRumorReward(getter, args.RewardID, args.LevelID, args.FactionID, args.Actor);
        UpdateUserInterface(args.Actor, ent);
    }

    private void OnTransferRumor(Entity<JobNetComponent> ent, ref JobNetTransferRumorMessage args)
    {
        if (!TryComp<RumorGetterComponent>(ent, out var getter) || getter == null) return;
        _rumors.TransferRumorByIndex(getter, args.ID, args.Target, args.Actor);
        UpdateUserInterface(args.Actor, ent);
    }

    private void OnCancelRumor(Entity<JobNetComponent> ent, ref JobNetCancelRumorMessage args)
    {
        if (!TryComp<RumorGetterComponent>(ent, out var getter) || getter == null) return;
        _rumors.CancelRumorByIndex(getter, args.ID);
        UpdateUserInterface(args.Actor, ent);
    }
    public JobNetComponent? GetJobNetByName(string name)
    {
        var query = EntityQueryEnumerator<JobNetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var tranform = Transform(uid);
            var parent = tranform.ParentUid;
            if (Name(parent) == name)
            {
                return comp;
            }
        }
        return null;
    }

    private void AfterInteractOn(EntityUid uid, PrecursorExtractorComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp(args.Target, out ImplantedComponent? implanted))
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, (EntityUid)args.Target))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.DoAfter, new PrecursorExtractorDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnDoAfter(EntityUid uid, PrecursorExtractorComponent component, PrecursorExtractorDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        if (!TryComp<ActorComponent>(args.User, out var actor) || actor.PlayerSession == null)
            return;

        if (args.Args.Target != null)
        {
            var target = (EntityUid)args.Args.Target;
            bool vulnerable = false;
            if (TryComp<CuffableComponent>(target, out var cuffable) && cuffable != null)
            {
                if (_cuffable.IsCuffed((target, cuffable)))
                {
                    vulnerable = true;
                }
            }

            if (TryComp<MobStateComponent>(target, out var mobState))
            {
                if (mobState.CurrentState == MobState.Dead)
                {
                    vulnerable = true;
                }
                if (mobState.CurrentState == MobState.Critical)
                {
                    vulnerable = true;
                }
            }
            if (vulnerable)
            {
                if (TryComp<ImplantedComponent>(target, out var implanted))
                {
                    foreach (var implant in implanted.ImplantContainer.ContainedEntities)
                    {
                        if (TryComp<JobNetComponent>(implant, out var comp))
                        {
                            var msg = $"The secret phrase for {Name(target)} is: {comp.SecretPhrase}";
                            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Notifications,
                            msg,
                            msg,
                            uid,
                            false,
                            actor.PlayerSession.Channel
                            );
                        }
                    }
                }
            }
            else
            {
                var msg = $"This target is not vulnerable to precursor extraction.";
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Notifications,
                    msg,
                    msg,
                    uid,
                    false,
                    actor.PlayerSession.Channel
                    );
            }

            args.Handled = true;
        }
    }

    private void OnPurchase(EntityUid uid, JobNetComponent component, JobNetPurchaseMessage args)
    {
        ProtoId<NetworkLevelPrototype> currentLevel = "NetworkLevel1";
        if (_meta.MetaRecords == null) return;
        if (_meta.MetaRecords.TryGetRecord(Name(args.Actor), out var record) && record != null)
        {
            currentLevel = record.Level;
        }
        else return;
        _proto.Resolve(currentLevel, out var currentProto);
        if (currentProto == null) return;
        if (currentProto.Next == null) return;
        _proto.Resolve(currentProto.Next, out var nextProto);
        if (nextProto == null) return;
        int cost = nextProto.Cost;
        if (_bank.TryGetBalance(args.Actor, out var balance))
        {
            if (cost > balance) return;
            if (_bank.TryBankWithdraw(args.Actor, cost))
            {
                record.Level = nextProto.ID;
            }
        }
        UpdateUserInterface(args.Actor, uid, component);

    }

    private void OnSelect(EntityUid uid, JobNetComponent component, JobNetSelectMessage args)
    {
        var station = _station.GetStationByID(args.ID);
        if (station == null || args.ID == 0)
        {
            var currentWorkingFor = component.WorkingFor;
            component.WorkingFor = 0;
            if (currentWorkingFor != 0 && currentWorkingFor != null)
            {
                var sId = _station.GetStationByID(currentWorkingFor.Value);
                if (sId != null) _crewManifest.BuildCrewManifest(sId.Value);
            }
            if (component.WorkingFor != 0 && component.WorkingFor != null)
            {
                var sId = _station.GetStationByID(component.WorkingFor.Value);
                if (sId != null) _crewManifest.BuildCrewManifest(sId.Value);
            }
            _card.UpdateIDAssignment(Name(args.Actor), args.ID);
            UpdateUserInterface(args.Actor, uid, component);
            return;
        }

        if (TryComp<CrewRecordsComponent>(station, out var crewRecord) && crewRecord != null)
        {
            if (crewRecord.TryGetRecord(Name(args.Actor), out var record) && record != null)
            {
                if (TryComp<StationDataComponent>(station, out var stationData))
                {
                    if (TryComp<CrewAssignmentsComponent>(station, out var crewAssignments))
                    {
                        if (crewAssignments.TryGetAssignment(record.AssignmentID, out var assignment) && assignment != null)
                        {
                            if (component.LastWorkedFor != stationData.UID)
                                component.WorkedTime = TimeSpan.Zero;
                            var currentWorkingFor = component.WorkingFor;
                            component.WorkingFor = stationData.UID;
                            if (currentWorkingFor != 0 && currentWorkingFor != null)
                            {
                                var sId = _station.GetStationByID(currentWorkingFor.Value);
                                if (sId != null) _crewManifest.BuildCrewManifest(sId.Value);
                            }
                            if (component.WorkingFor != 0 && component.WorkingFor != null)
                            {
                                var sId = _station.GetStationByID(component.WorkingFor.Value);
                                if (sId != null) _crewManifest.BuildCrewManifest(sId.Value);
                            }
                            _card.UpdateIDAssignment(Name(args.Actor), args.ID);
                            UpdateUserInterface(args.Actor, uid, component);
                        }
                    }
                }
            }
        }

    }
    private void OnJobNetOpenAttempt(EntityUid uid, JobNetComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (!_mind.TryGetMind(args.User, out var mind, out _))
            return;

        _popup.PopupEntity("Job Network Not Available.", uid, args.User);
        args.Cancel();
    }

    private void OnMapInit(EntityUid uid, JobNetComponent component, MapInitEvent args)
    {

    }

    private void OnStartup(EntityUid uid, JobNetComponent component, ComponentStartup args)
    {
        var currentWorkingFor = component.WorkingFor;
        if (currentWorkingFor != 0 && currentWorkingFor != null)
        {
            var sId = _station.GetStationByID(currentWorkingFor.Value);
            if (sId == null) return;
            var jobNetEnabled = _station.GetJobNetStatus(sId.Value);
            if (!jobNetEnabled)
            {
                component.WorkingFor = 0;
            }
            else
            {
                _crewManifest.BuildCrewManifest(sId.Value);
            }
        }
    }

    private void OnShutdown(EntityUid uid, JobNetComponent component, ComponentShutdown args)
    {
        var currentWorkingFor = component.WorkingFor;
        component.WorkingFor = 0;
        if (currentWorkingFor != 0 && currentWorkingFor != null)
        {
            var sId = _station.GetStationByID(currentWorkingFor.Value);
            if (sId != null) _crewManifest.BuildCrewManifest(sId.Value);
        }
        component.WorkingFor = currentWorkingFor;
    }

    private void OnImplantActivate(EntityUid uid, JobNetComponent component, OpenJobNetImplantEvent args)
    {
        ToggleUi(args.Performer, uid, component);
    }

    private void OnJobNetImplantImplanted(Entity<JobNetComponent> ent, ref ImplantImplantedEvent args)
    {
        var mob = args.Implanted;
        _bank.EnsureAccount(Name(mob), 500);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<JobNetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.WorkingFor != null && comp.WorkingFor != 0)
            {
                comp.WorkedTime += TimeSpan.FromSeconds(frameTime);
                if (comp.WorkedTime > TimeSpan.FromMinutes(20))
                {
                    comp.WorkedTime = TimeSpan.Zero;
                    TryPay(comp.Owner, comp);
                }
            }
        }
        base.Update(frameTime);
    }
    public void NotifyPlayer(EntityUid player, string msg, SoundSpecifier? sound = null)
    {
        if (TryComp<ActorComponent>(player, out var actor) && actor != null && actor.PlayerSession != null)
        {
            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Notifications,
                msg,
                msg,
                player,
                false,
                actor.PlayerSession.Channel
                );
            if (sound != null)
            {
                _globalsound.PlayLocalSound(sound, actor);
            }
        }
    }
    public void TryPay(EntityUid user, JobNetComponent component)
    {

        if (component.WorkingFor == null || component.WorkingFor == 0) return;
        var station = _station.GetStationByID(component.WorkingFor.Value);
        if (station == null)
        {
            component.WorkingFor = 0;
            return;
        }
        EntityUid? player = null;
        var comp = Transform(user);
        if (comp != null)
        {
            player = comp.ParentUid;
        }
        if (player == null) return;
        var name = Name(player.Value);
        if (TryComp<CrewRecordsComponent>(station, out var crewRecord) && crewRecord != null)
        {
            if (crewRecord.TryGetRecord(name, out var record) && record != null)
            {
                if (TryComp<StationDataComponent>(station, out var stationData))
                {
                    if (TryComp<CrewAssignmentsComponent>(station, out var crewAssignments))
                    {
                        if (crewAssignments.TryGetAssignment(record.AssignmentID, out var assignment) && assignment != null)
                        {
                            if (assignment.Wage > 0)
                            {
                                if (TryComp<ActorComponent>(player, out var actor) && actor != null && actor.PlayerSession != null)
                                {
                                    record.LastPaid = DateTime.Now;
                                    var bank = _bank.GetMoneyAccountsComponent();
                                    if (bank == null) return;
                                    if (_cargo.TryGetAccount(station.Value, "Cargo", out var money))
                                    {
                                        if (money < assignment.Wage)
                                        {
                                            var msg = $"{stationData.StationName} has failed to pay you your ${assignment.Wage} due to insufficient funds.";
                                            NotifyPlayer(player.Value, msg, component.ErrorSound);
                                            return;
                                        }
                                        if (bank.TryGetAccount(name, out var account) && account != null)
                                        {
                                            account.Balance += assignment.Wage;
                                            _cargo.TryAdjustBankAccount(station.Value, "Cargo", -assignment.Wage);
                                            var msg = $"You have received ${assignment.Wage} for working as a {assignment.Name} for {stationData.StationName}.";
                                            NotifyPlayer(player.Value, msg, component.PaySuccessSound);
                                            _bank.DirtyMoneyAccountsComponent();
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        var msg = $"{stationData.StationName} has failed to pay you your ${assignment.Wage} due to an invalid account.";
                                        NotifyPlayer(player.Value, msg, component.ErrorSound);
                                        return;
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
