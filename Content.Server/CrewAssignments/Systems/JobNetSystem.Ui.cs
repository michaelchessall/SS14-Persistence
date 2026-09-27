using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.CrewRecords.Systems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.CrewAssignments;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.CrewAssignments.Prototypes;
using Content.Shared.CrewAssignments.Systems;
using Content.Shared.CrewRecords.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Station.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.CrewAssignments.Systems;

public sealed partial class JobNetSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CrewMetaRecordsSystem _meta = default!;

    private void InitializeUi()
    {
        SubscribeLocalEvent<JobNetComponent, JobNetRequestUpdateInterfaceMessage>(OnRequestUpdate);
    }




    public void ToggleUi(EntityUid user, EntityUid jobnetEnt, JobNetComponent? component = null)
    {
        if (!Resolve(jobnetEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!_ui.TryToggleUi(jobnetEnt, JobNetUiKey.Key, actor.PlayerSession))
            return;

        UpdateUserInterface(user, jobnetEnt, component);
    }


    public void CloseUi(EntityUid uid, JobNetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _ui.CloseUi(uid, JobNetUiKey.Key);
    }

    public (string? jobTitle, string? factionName) GetJobNetStrings(EntityUid? user)
    {
        if (!TryComp<ImplantedComponent>(user, out var implanted)) return (null, null);

        EntityUid? jobNet = null;
        JobNetComponent? component = null;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (TryComp<JobNetComponent>(implant, out var comp))
            {
                jobNet = implant;
                component = comp;
            }
        }

        if (component == null || jobNet == null)
            return (null, null);

        var stations = _station.GetStationsSet();
        string? jobTitle = null;
        string? factionName = null;
        foreach (var station in stations)
        {
            if (!TryComp<CrewRecordsComponent>(station, out var crewRecord)
                || (!crewRecord.TryGetRecord(Name(user.Value), out var record)
                || record == null)
                || !TryComp<StationDataComponent>(station, out var stationData)
                || stationData.StationName == null
                || (component.WorkingFor == null || component.WorkingFor == 0)
                || !TryComp<CrewAssignmentsComponent>(station, out var crewAssignments))
                continue;

            if (stationData.UID == component.WorkingFor)
            {
                factionName = stationData.StationName;
            }

            if (crewAssignments.TryGetAssignment(record.AssignmentID, out var assignment) &&
                assignment != null)
            {
                jobTitle = assignment.Name;
            }
        }

        return (jobTitle, factionName);
    }

    public void UpdateUserInterface(EntityUid? user, EntityUid jobnet, JobNetComponent? component = null)
    {
        if (!Resolve(jobnet, ref component) || user == null || component == null)
            return;

        Dictionary<int, string> possibleStations = new Dictionary<int, string>();
        var stations = _station.GetStationsSet();
        string? assignmentName = null;
        int? wage = null;
        int selectedstation = 0;
        TimeSpan remainingTime = TimeSpan.FromMinutes(20) - component.WorkedTime;
        var spendAuth = false;
        var spent = 0;
        var spendable = 0;
        var sectorChaos = 0;
        var sectorStatus = "";
        foreach (var station in stations)
        {
            if (TryComp<CrewRecordsComponent>(station, out var crewRecord) && crewRecord != null)
            {
                if (crewRecord.TryGetRecord(Name(user.Value), out var record) && record != null)
                {
                    if (TryComp<StationDataComponent>(station, out var stationData))
                    {
                        if (stationData.StationName == null) continue;
                        if (stationData.JobNetEnabled)
                        {
                            possibleStations.Add(stationData.UID, stationData.StationName);
                        }
                        if (component.WorkingFor != null && component.WorkingFor != 0)
                        {
                            if (stationData.UID == component.WorkingFor)
                            {
                                if (TryComp<CrewAssignmentsComponent>(station, out var crewAssignments))
                                {
                                    if (crewAssignments.TryGetAssignment(record.AssignmentID, out var assignment) && assignment != null)
                                    {
                                        assignmentName = assignment.Name;
                                        wage = assignment.Wage;
                                        selectedstation = stationData.UID;
                                        if (_station.CanSpend(record.Name, station))
                                        {
                                            spendAuth = true;
                                            spent = record.Spent;
                                            spendable = assignment.SpendingLimit;
                                        }
                                        if (_station.IsOwner(record.Name, station))
                                        {
                                            spent = 0;
                                            spendable = 99999999;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


        }
        List<WorldObjectivesEntry> currentObjectives;
        List<WorldObjectivesEntry> completedObjectives;
        List<CodexEntry> codexEntries;
        ProtoId<NetworkLevelPrototype> currentLevel = "NetworkLevel1";
        if (_meta.MetaRecords != null)
        {
            completedObjectives = _meta.MetaRecords.CompletedObjectives;
            currentObjectives = _meta.MetaRecords.CurrentObjectives;
            codexEntries = _meta.MetaRecords.CodexEntries;
            if (_meta.MetaRecords.TryGetRecord(Name(user.Value), out var record) && record != null)
            {
                currentLevel = record.Level;
            }
            sectorChaos = _meta.MetaRecords.SectorChaos;
            sectorStatus = _meta.MetaRecords.SectorStatus;
        }
        else
        {
            completedObjectives = new();
            currentObjectives = new();
            codexEntries = new();
        }
        var balance = 0;
        _bank.TryGetBalance(user.Value, out balance);
        string? stationName = null;
        if(component.DealerBounty != null)
        {
            if(component.DealerBounty.TradeStationUID != 0)
            {
                var ts = _cargo.GetTradeStationByID(component.DealerBounty.TradeStationUID);
                if (ts != null)
                {
                    stationName = Name(ts.Value);
                }
            }
        }

        var state = new JobNetUpdateState(possibleStations, assignmentName, wage, selectedstation, remainingTime, currentObjectives, completedObjectives, codexEntries, currentLevel, balance, spendAuth, spent, spendable, component.Precursor, component.PrecursorObjectives, component.PrecursorResetTime, component.RogueLevel, component.XP, component.NetworkType, component.SecretPhrase, component.KillTarget, component.DealerBounty, stationName, component.RogueNetResetTime, sectorChaos, _cargo.GetSectorDevelopment(), sectorStatus);
        _ui.SetUiState(jobnet, JobNetUiKey.Key, state);
    }

    private void OnRequestUpdate(EntityUid uid, JobNetComponent component, JobNetRequestUpdateInterfaceMessage args)
    {
        UpdateUserInterface(args.Actor, GetEntity(args.Entity), component);
    }

    private void BeforeActivatableUiOpen(EntityUid uid, JobNetComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(args.User, uid, component);
    }
}
