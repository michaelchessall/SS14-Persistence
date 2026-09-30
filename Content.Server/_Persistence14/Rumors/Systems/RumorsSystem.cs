using Content.Server._NF.Bank;
using Content.Server.Administration.Managers;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.CrewAssignments.Systems;
using Content.Server.CrewRecords.Systems;
using Content.Server.Database;
using Content.Server.Database.Migrations.Postgres;
using Content.Server.Mind;
using Content.Server.Movement.Systems;
using Content.Server.NameIdentifier;
using Content.Server.Power.SMES;
using Content.Server.Salvage.Magnet;
using Content.Server.Station.Systems;
using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.Rumors.Components;
using Content.Shared._Persistence14.Rumors.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Atmos.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Prayer;
using Content.Shared.Station.Components;
using NetCord;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Content.Shared._Persistence14.Rumors.Systems;

public sealed partial class RumorsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private CrewMetaRecordsSystem _crewMeta = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MapBoundsSystem _mapBounds = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<SalvageMobRestrictionsComponent> _salvMobQuery = default!;

    [Dependency] private EntityQuery<RumorPowerTargetComponent> _powerTargetQuery = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private PersistentIdentifierSystem _pid = default!;
    [Dependency] private BankSystem _bank = default!;
    [Dependency] private NameIdentifierSystem _nameIdentifier = default!;
    [Dependency] private FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private JobNetSystem _jobnet = default!;
    [Dependency] private StationSystem _station = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RumorGetterComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RumorGetterComponent, GridUidChangedEvent>(OnGridChanged);
        SubscribeLocalEvent<RumorExterminationTargetComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RumorPowerTargetComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<EdibleComponent, IngestedRumorEvent>(OnEdibleIngested);
        SubscribeLocalEvent<PrayableComponent, PrayedEvent>(OnPrayed);
    }

    private void OnPrayed(Entity<PrayableComponent> ent, ref PrayedEvent args)
    {
        var grid = Transform(args.User).GridUid;
        if (grid == null) return;
        if (!TryComp<PersistentIdentifierComponent>(grid, out var pid) || pid == null) return;
        var getter = GetRumorComponent(args.User);
        if (getter == null) return;
        foreach (var rumor in getter.Value.Comp.Rumors.ShallowClone())
        {
            if (rumor.CompletionType == CompletionType.Pray)
            {
                if (rumor.Targets[0] == pid.Id)
                {
                    CompleteRumor(getter.Value, args.User, rumor);
                }
            }
        }
    }

    private void OnEdibleIngested(Entity<EdibleComponent> ent, ref IngestedRumorEvent args)
    {
        var grid = Transform(args.Target).GridUid;
        if (grid == null) return;
        if (!TryComp<PersistentIdentifierComponent>(grid, out var pid) || pid == null) return;
        var getter = GetRumorComponent(args.Target);
        if (getter == null) return;
        var flavors = _flavorProfile.GetAllFlavors(ent.Owner, args.Split);
        foreach (var rumor in getter.Value.Comp.Rumors.ShallowClone())
        {
            if (rumor.CompletionType == CompletionType.Eat)
            {
                if (ent.Comp.Edible != "Food") continue;
            }
            else if (rumor.CompletionType == CompletionType.Drink)
            {

                if (ent.Comp.Edible != "Drink") continue;
            }
            else
            {
                continue;
            }
            if (!rumor.Targets.Contains(pid.Id)) continue;
            bool passed = true;
            foreach (var flavor in rumor.TargetFlavors)
            {
                var found = false;
                foreach (var secondFlavor in flavors)
                {
                    if (secondFlavor.Id == flavor.Id)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) passed = false;
            }
            if (passed)
            {
                rumor.CurrentConsumption += args.Split.Volume.Int();
                var originalProto = _protoMan.Index<RumorPrototype>(rumor.OriginalPrototype);
                if (rumor.CurrentConsumption >= originalProto.ConsumptionTarget)
                {
                    CompleteRumor(getter.Value, args.Target, rumor);
                }
            }
        }

    }

    private void OnBatteryChargeChanged(Entity<RumorPowerTargetComponent> ent, ref ChargeChangedEvent args)
    {
        if (!TryComp<BatteryComponent>(ent, out var batteryComp)) return;
        if (args.CurrentCharge == batteryComp.MaxCharge)
        {
            var query = EntityQueryEnumerator<RumorGetterComponent>();
            if (!TryComp<PersistentIdentifierComponent>(ent, out var pid)) return;
            while (query.MoveNext(out var uid, out var comp))
            {
                foreach (var rumor in comp.Rumors.ShallowClone())
                {
                    if (rumor.Targets.Contains(pid.Id))
                    {
                        if (rumor.Targets.Count == 1)
                        {
                            EntityUid? player = null;
                            var implant = Transform(uid);
                            player = implant.ParentUid;
                            if (player == null) return;
                            CompleteRumor((uid, comp), player.Value, rumor);
                        }
                        else
                        {
                            bool pass = true;
                            foreach (var target in rumor.Targets)
                            {
                                if (target == pid.Id) continue;
                                if (!_pid.TryResolveId(target, out var sister)) return;
                                if (!TryComp<BatteryComponent>(sister, out var sisterBattery) || sisterBattery == null) return;
                                if (sisterBattery.LastCharge < sisterBattery.MaxCharge)
                                {
                                    pass = false;
                                }

                            }
                            if (pass)
                            {
                                EntityUid? player = null;
                                var implant = Transform(uid);
                                player = implant.ParentUid;
                                if (player == null) return;
                                CompleteRumor((uid, comp), player.Value, rumor);
                            }
                        }
                    }
                }
            }
        }
    }

    public void CompleteBounty(RumorGetterComponent comp, CargoBountyData bounty)
    {
        foreach (var rumor in comp.Rumors)
        {
            if (rumor.Bounty != null && rumor.Bounty.Id == bounty.Id)
            {
                EntityUid? player = null;
                var implant = Transform(comp.Owner);
                player = implant.ParentUid;
                if (player == null) return;
                CompleteRumor((comp.Owner, comp), player.Value, rumor);
                return;
            }
        }
    }
    public RumorGetterComponent? GetRumorGetterByName(string name)
    {
        var query = EntityQueryEnumerator<RumorGetterComponent>();
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
    private void OnMobStateChanged(Entity<RumorExterminationTargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (!TryComp<PersistentIdentifierComponent>(ent, out var pid)) return;
        if (args.NewMobState == MobState.Dead)
        {
            var query = EntityQueryEnumerator<RumorGetterComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                foreach (var rumor in comp.Rumors.ShallowClone())
                {
                    if (rumor.Targets.Contains(pid.Id))
                    {
                        rumor.Targets.Remove(pid.Id);
                        if (rumor.Targets.Count == 0)
                        {
                            EntityUid? player = null;
                            var implant = Transform(uid);
                            player = implant.ParentUid;
                            if (player == null) return;
                            CompleteRumor((uid, comp), player.Value, rumor);
                        }
                    }
                }
            }
        }
    }

    private void OnGridChanged(Entity<RumorGetterComponent> ent, ref GridUidChangedEvent args)
    {
        EntityUid? player = null;
        var implant = Transform(ent);
        player = implant.ParentUid;
        if (player == null) return;
        if (args.NewGrid == null) return;
        foreach (var rumor in ent.Comp.Rumors)
        {
            if (rumor.CompletionType == CompletionType.Discover)
            {
                if (TryComp<PersistentIdentifierComponent>(args.NewGrid.Value, out var pidComp))
                {
                    if (_pid.TryGetId((args.NewGrid.Value, pidComp), out var pid))
                    {
                        if (rumor.Targets.Contains(pid))
                        {
                            rumor.Targets.Remove(pid);
                            if (rumor.Targets.Count == 0)
                            {
                                CompleteRumor(ent, player.Value, rumor);
                            }
                            else
                            {
                                NotifyPlayer(player.Value, $"You have completed a part of the {rumor.Name} rumor! {rumor.Targets.Count} more to go!");
                            }
                        }
                    }
                }
            }
        }
    }

    private void CompleteRumor(Entity<RumorGetterComponent> ent, EntityUid player, ActiveRumor rumor)
    {
        if (_crewMeta.MetaRecords == null) return;
        var name = Name(player);
        _crewMeta.MetaRecords.TryGetRecord(name, out var metaRecord);
        if (metaRecord == null) return;
        var reputation = 0;
        metaRecord.MetaFactionReputations.TryGetValue(rumor.Faction, out var rep);
        reputation += rep;
        metaRecord.MetaFactionReputations[rumor.Faction] = reputation + rumor.ReputationReward;
        int finalCashReward = rumor.CashReward;
        if (rumor.CashReward > 0)
        {
            var rumorTax = 0;
            EntityUid? taxingStation = null;
            if (TryComp<JobNetComponent>(ent, out var jobnet) && jobnet != null)
            {
                if (jobnet.WorkingFor != null && jobnet.WorkingFor != 0)
                {
                    var sId = _station.GetStationByID(jobnet.WorkingFor.Value);
                    if (sId != null)
                    {
                        if (TryComp<StationDataComponent>(sId, out var sD) && sD != null)
                        {
                            rumorTax = sD.SalesTax;
                            taxingStation = sId;
                        }
                    }
                }
            }
            if(rumorTax > 0)
            {
                float taxmult = (float)rumorTax / 100f;
                var taxpaid = (float)finalCashReward * taxmult;
                var taxPaidInt = (int)Math.Round(taxpaid);
                finalCashReward -= taxPaidInt;
                if (taxPaidInt > 0)
                {
                    if (taxingStation != null)
                    {
                        if (TryComp<StationBankAccountComponent>(taxingStation, out var taxBankAccount) && taxBankAccount != null)
                        {
                            _cargo.UpdateBankAccount((taxingStation.Value, taxBankAccount), taxPaidInt, "Cargo");
                        }
                    }
                }
            }
            var bank = _bank.GetMoneyAccountsComponent();
            if (bank == null) return;
            if (bank.TryGetAccount(name, out var account) && account != null)
            {
                account.Balance += rumor.CashReward;
            }

        }
        string msg = $"You have completed the {rumor.Name} rumor!";
        if(rumor.ReputationReward > 0)
        {
            msg += $"\nYou have gained {rumor.ReputationReward} reputation with the {rumor.Faction}";
        }
        if (finalCashReward > 0)
        {
            msg += $"\nYou have earned ${finalCashReward}";
        }
        NotifyPlayer(player, msg, new SoundPathSpecifier("/Audio/Effects/kaching.ogg"), ent);
        var originalProto = _protoMan.Index<RumorPrototype>(rumor.OriginalPrototype);
        if (originalProto.RewardRumors > 0)
        {
            var rumorsToSpawn = _random.GetItems(originalProto.PossibleFollowups, originalProto.RewardRumors);
            var faction = _protoMan.Index<MetaFactionPrototype>(rumor.Faction);
            foreach (var followup in rumorsToSpawn)
            {

                var final = RealizeRumor(followup, faction);
                AssignRumor(ent.Comp, player, rumor.Faction, final);
            }
        }
        ent.Comp.Rumors.Remove(rumor);
    }

    private void OnComponentInit(Entity<RumorGetterComponent> ent, ref ComponentInit args)
    {
        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
    }

    public override void Update(float frameTime)
    {
        var gridDeleteQuery = EntityQueryEnumerator<GridSelfDeleteComponent>();
        while (gridDeleteQuery.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.DeleteTime)
            {
                SelfDeleteGrid(uid);
            }
        }
        var query = EntityQueryEnumerator<RumorGetterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            EntityUid? player = null;
            var implant = Transform(uid);
            player = implant.ParentUid;
            if (player == null) return;
            foreach (var rumor in comp.Rumors.ShallowClone())
            {
                if(rumor.DebugComplete)
                {
                    CompleteRumor((uid, comp), player.Value, rumor);
                    continue;
                }

                if (rumor.ShouldSpawn)
                {
                    TrySpawnRumor(player.Value, rumor);
                }
            }
            comp.NextRumor -= TimeSpan.FromSeconds(frameTime);
            if (comp.NextRumor <= TimeSpan.Zero)
            {
                comp.NextRumor = comp.RumorCooldownLength;
                AssignRumor(comp, player.Value);
            }
        }
        base.Update(frameTime);
    }

    private void SelfDeleteGrid(EntityUid uid)
    { 
        var mobQuery = AllEntityQuery<MobStateComponent, TransformComponent>();
        List<(Entity<TransformComponent> Entity, EntityUid MapUid, Vector2 LocalPosition)> detachEnts = new();
        while (mobQuery.MoveNext(out var mobUid, out _, out var xform))
        {
            if (xform.GridUid == null || xform.GridUid != uid || xform.MapUid == null)
                continue;

            if (_salvMobQuery.HasComp(mobUid))
                continue;
            detachEnts.Add(((mobUid, xform), xform.MapUid.Value, _transform.GetWorldPosition(xform)));
            _transform.DetachEntity(mobUid, xform);
        }
        QueueDel(uid);
        foreach(var entity in detachEnts)
            {
            _transform.SetCoordinates(entity.Entity.Owner, new EntityCoordinates(entity.MapUid, entity.LocalPosition));
        }

    }

    public Entity<RumorGetterComponent>? GetRumorComponent(EntityUid player)
    {
        var query = EntityQueryEnumerator<RumorGetterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            EntityUid? playerMaybe = null;
            var implant = Transform(uid);
            playerMaybe = implant.ParentUid;
            if (playerMaybe == player) return (uid, comp);
        }
        return null;
    }
    public void AssignRumor(RumorGetterComponent comp, EntityUid player, ProtoId<MetaFactionPrototype>? factionId = null, ActiveRumor? newRumor = null)
    {
        if(newRumor == null) newRumor = GenerateRumor(player, comp, factionId);
        if (newRumor != null)
        {
            comp.Rumors.Add(newRumor);
            NotifyPlayer(player, $"Your job network reports you have recieved a new rumor: {newRumor.Name}");
        }
        _jobnet.UpdateUserInterface(player, comp.Owner);
    }

    public void NotifyPlayer(EntityUid player, string msg, SoundSpecifier? sound = null, EntityUid? source = null)
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
        }
        if (sound != null && source != null)
        {
            _audio.PlayEntity(sound, player, source.Value);
        }
    }
    private void TrySpawnRumor(EntityUid player, ActiveRumor rumor)
    {
        var targetPos = rumor.TargetPosition;
        var currentPos = Transform(player).MapPosition;
        if(targetPos == null) return;
        var dist = Vector2.Distance(targetPos.Value.Position, currentPos.Position);
        if (currentPos.MapId == targetPos.Value.MapId &&  dist <= 500)
        {
            NotifyPlayer(player, $"The grids from the {rumor.Name} rumor have appeared nearby!");
            SpawnEventGrids(rumor, player);
            rumor.ShouldSpawn = false;
        }
    }

    private ActiveRumor? GenerateRumor(EntityUid uid, RumorGetterComponent comp, ProtoId<MetaFactionPrototype>? factionId = null)
    {
        if (comp.Rumors.Count >= 4) return null;
        var metaFactions = _protoMan.EnumeratePrototypes<MetaFactionPrototype>().ToList();
        if (_crewMeta.MetaRecords == null) return null;
        _crewMeta.MetaRecords.TryGetRecord(Name(uid), out var metaRecord);
        if (metaRecord == null) return null;
        MetaFactionPrototype? chosenFaction = null;
        if (factionId != null)
        {
            chosenFaction = _protoMan.Index<MetaFactionPrototype>(factionId.Value);
        }
        else
        {
            List<MetaFactionPrototype> eligibleFactions = new List<MetaFactionPrototype>();
            foreach (var faction in metaFactions)
            {
                int reputation = 0;
                if (metaRecord.MetaFactionReputations.TryGetValue(faction.ID, out var rep))
                {
                    reputation = rep;
                }
                var levels = faction.Levels;
                eligibleFactions.Add(faction);
                foreach (var level in levels)
                {
                    if (reputation >= level.Value)
                    {
                        eligibleFactions.Add(faction);
                        break;
                    }
                }
            }
            chosenFaction = eligibleFactions[_random.Next(eligibleFactions.Count)];
        }
        if (chosenFaction == null) return null;
        int chosenReputation = 0;
        if (metaRecord.MetaFactionReputations.TryGetValue(chosenFaction.ID, out var rep2))
        {
            chosenReputation = rep2;
        }
        var chosenLevels = chosenFaction.Levels;
        List<ProtoId<RumorPrototype>> eligibleRumors = new List<ProtoId<RumorPrototype>>();
        eligibleRumors.AddRange(chosenFaction.Rumors);
        foreach (var kv in chosenLevels)
        {
            if(chosenReputation >= kv.Value)
            {
                var levelProto = _protoMan.Index<MetaFactionLevelPrototype>(kv.Key);
                eligibleRumors.AddRange(levelProto.Rumors);
            }
        }
        if(eligibleRumors.Count == 0) return null;
        var chosenRumor = eligibleRumors[_random.Next(eligibleRumors.Count)];

        return RealizeRumor(chosenRumor, chosenFaction);
    }

    private ActiveRumor? RealizeRumor(ProtoId<RumorPrototype> chosenRumor, MetaFactionPrototype chosenFaction)
    {
        if(_crewMeta.MetaRecords == null) return null;
        ActiveRumor final = new(chosenRumor);
        RumorPrototype rumorProto = _protoMan.Index<RumorPrototype>(chosenRumor);
        final.CompletionType = rumorProto.CompletionType;
        final.Name = $"{rumorProto.Name} ({chosenFaction.Name})";
        final.Faction = chosenFaction.ID;
        if(rumorProto.EventGrids.Count > 0)
        {
            final.ShouldSpawn = true;
            final.EventGrids.AddRange(_random.GetItems(rumorProto.EventGrids, rumorProto.GridsToSpawn));
            final.TargetPosition = _mapBounds.GetRandomInBounds(_crewMeta.MetaRecords.Owner);
            final.SpawnDistance = rumorProto.SpawnDistance;
            final.GridsToSpawn = rumorProto.GridsToSpawn;
        }
        if(rumorProto.PossibleBounties.Count > 0)
        {
            var chosenBounty = _random.Pick(rumorProto.PossibleBounties);
            var query = EntityQueryEnumerator<TradeStationComponent>();
            List<TradeStationComponent> possibleTrade = new();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (TryComp<StationMemberComponent>(uid, out var sm))
                {
                    possibleTrade.Add(comp);
                }
            }
            if (possibleTrade.Count < 1) return null;
            var chosenUID = _random.Pick(possibleTrade).UID;
            _nameIdentifier.GenerateUniqueNameModifier("Bounty", out var randomVal);
            var newBounty = new CargoBountyData(_protoMan.Index<CargoBountyPrototype>(chosenBounty), randomVal);
            newBounty.TradeStationUID = chosenUID;
            final.Bounty = newBounty;
        }
        if(rumorProto.CompletionType == CompletionType.Eat || rumorProto.CompletionType == CompletionType.Drink)
        {
            final.TargetFlavors.AddRange(_random.GetItems(rumorProto.PossibleFlavors, rumorProto.FlavorsToPick));
            var serviceQuery = EntityQueryEnumerator<RumorServiceStationComponent>();
            List<EntityUid> possibleService = new();
            while (serviceQuery.MoveNext(out var uid, out var comp))
            {
                if (comp.Tag != rumorProto.TargetTag) continue;
                possibleService.Add(uid);
            }
            if (possibleService.Count < 1) return null;
            final.Targets.Add(_pid.EnsureId(_random.Pick(possibleService)));
        }
        if(rumorProto.CompletionType == CompletionType.Pray)
        {
            var prayerQuery = EntityQueryEnumerator<RumorPrayerStationComponent>();
            List<EntityUid> possibleService = new();
            while (prayerQuery.MoveNext(out var uid, out var comp))
            {
                if (comp.Tag != rumorProto.TargetTag) continue;
                possibleService.Add(uid);
            }
            if (possibleService.Count < 1) return null;
            final.Targets.Add(_pid.EnsureId(_random.Pick(possibleService)));
        }
        final.CashReward = rumorProto.CashReward;
        final.ReputationReward = rumorProto.ReputationReward;
        final.Description = RealizeDescription(rumorProto, chosenFaction, final);
        return final;
    }

    private string RealizeDescription(RumorPrototype rumor, MetaFactionPrototype chosenFaction, ActiveRumor active)
    {
        string addon = "";

        if (active.GridsToSpawn > 1 && active.EventGrids.Count > 0)
        {
            if (active.TargetPosition == null) return rumor.Description;
            addon += $"\n{active.GridsToSpawn} grids will arrive as you approach\n[color=yellow]{Math.Round(active.TargetPosition.Value.Position.X)}, {Math.Round(active.TargetPosition.Value.Position.Y)})";
        }
        else if(active.GridsToSpawn == 1 && active.EventGrids.Count > 0)
        {
            if (active.TargetPosition == null) return rumor.Description;
            addon += $"\nA grid will arrive as you approach\n[color=yellow]({Math.Round(active.TargetPosition.Value.Position.X)}, {Math.Round(active.TargetPosition.Value.Position.Y)})[/color]";
        }
        if(rumor.CompletionType == CompletionType.Eat || rumor.CompletionType == CompletionType.Drink)
        {
            if (active.Targets.Count < 1) return rumor.Description;
            if(rumor.CompletionType == CompletionType.Eat)
            {
                addon += $"\nEat something that tastes";
            }
            else if (rumor.CompletionType == CompletionType.Drink)
            {
                addon += $"\nDrink something that tastes";
            }

            bool first = true;
            foreach(var flavor in active.TargetFlavors)
            {
                var flavorProto = _protoMan.Index<FlavorPrototype>(flavor);
                addon += $" {Loc.GetString(flavorProto.FlavorDescription)}";
                if(!first)
                {
                    addon += ",";
                }
            }
            if (!_pid.TryResolveId(active.Targets[0], out var targetStation) || targetStation == null) return rumor.Description; 
            addon += $" while onboard {Name(targetStation)}";
        }
        if(rumor.CompletionType == CompletionType.Pray)
        {
            addon += $"\nPray or reflect at an altar ";
            if (!_pid.TryResolveId(active.Targets[0], out var targetStation) || targetStation == null) return rumor.Description;
            addon += $"while onboard {Name(targetStation)}";
        }
        if(rumor.CompletionType == CompletionType.Bounty)
        {
            if (active.Bounty == null) return rumor.Description;
            var ts = _cargo.GetTradeStationByID(active.Bounty.TradeStationUID);
            if (ts == null) return rumor.Description;
            addon += $"\nComplete the bounty {active.Bounty.Id}";
            addon += $" available at {Name(ts.Value)}";
        }
        addon += rumor.DescriptionAddon;
        
        return rumor.Description + addon;
    }

    private void SpawnEventGrids(ActiveRumor rumor, EntityUid player)
    {
        var originalProto = _protoMan.Index<RumorPrototype>(rumor.OriginalPrototype);
        var salvMap = _mapSystem.CreateMap();
        var salvMapXform = Transform(salvMap);
        foreach (var gridPath in rumor.EventGrids)
        {
            if (!_loader.TryLoadGrid(salvMapXform.MapID, gridPath, out _))
            {
                _mapSystem.DeleteMap(salvMapXform.MapID);
                return;
            }
        }

        Box2? bounds = null;
        if (salvMapXform.ChildCount == 0)
        {
            return;
        }

        var mapChildren = salvMapXform.ChildEnumerator;

        while (mapChildren.MoveNext(out var mapChild))
        {
            // If something went awry in dungen.
            if (!_gridQuery.TryGetComponent(mapChild, out var childGrid))
                continue;

            var childAABB = _transform.GetWorldMatrix(mapChild).TransformBox(childGrid.LocalAABB);
            bounds = bounds?.Union(childAABB) ?? childAABB;
        }

        var magnetXform = Transform(player);
        var magnetGridUid = magnetXform.GridUid;
        var attachedBounds = new Box2Rotated();
        var mapId = magnetXform.MapID;
        Angle worldAngle;
        if (magnetGridUid != null)
        {
            var magnetGridXform = Transform(magnetGridUid.Value);
            var (gridPos, gridRot) = _transform.GetWorldPositionRotation(magnetGridXform);
            var gridAABB = _gridQuery.GetComponent(magnetGridUid.Value).LocalAABB;

            attachedBounds = new Box2Rotated(gridAABB.Translated(gridPos), gridRot, gridPos);

            worldAngle = (gridRot + magnetXform.LocalRotation) - MathF.PI / 2;

        }
        else
        {
            worldAngle = _random.NextAngle();
        }

        if (!TryGetGridSpawnPosition(player, rumor, mapId, attachedBounds, bounds!.Value, worldAngle, out var spawnLocation, out var spawnAngle))
        {
            _mapSystem.DeleteMap(salvMapXform.MapID);
            return;
        }

        // I have no idea if we want to return on failure or not
        // but I assume trying to set the parent with a null value wouldn't have worked out anyways
        if (!_mapSystem.TryGetMap(spawnLocation.MapId, out var spawnUid))
            return;

        mapChildren = salvMapXform.ChildEnumerator;

        // It worked, move it into position and cleanup values.
        while (mapChildren.MoveNext(out var mapChild))
        {
            if(rumor.CompletionType == CompletionType.Discover)
            {
                var pid = _pid.EnsureId(mapChild);
                rumor.Targets.Add(pid);
            }
            if(originalProto.DespawnGrids)
            {
                var comp = EnsureComp<GridSelfDeleteComponent>(mapChild);
                comp.DeleteTime = originalProto.GridLifetime + _timing.CurTime;
            }
            
            var salvXForm = Transform(mapChild);
            var localPos = salvXForm.LocalPosition;

            _transform.SetParent(mapChild, salvXForm, spawnUid.Value);
            _transform.SetWorldPositionRotation(mapChild, spawnLocation.Position + localPos, spawnAngle, salvXForm);

            // Handle mob restrictions
            var children = salvXForm.ChildEnumerator;

            while (children.MoveNext(out var child))
            {
                if (_salvMobQuery.TryGetComponent(child, out var salvMob))
                {
                    salvMob.LinkedEntity = mapChild;
                    if (rumor.CompletionType == CompletionType.Exterminate)
                    {
                        rumor.Targets.Add(_pid.EnsureId(child));
                        EnsureComp<RumorExterminationTargetComponent>(child);
                    }
                }

                if(rumor.CompletionType == CompletionType.Power)
                {
                    if (!_powerTargetQuery.TryGetComponent(child, out _))
                        continue;
                    rumor.Targets.Add(_pid.EnsureId(child));
                }
                
            }
        }
        _mapSystem.DeleteMap(salvMapXform.MapID);

    }

    private bool TryGetGridSpawnPosition(EntityUid player, ActiveRumor rumor, MapId mapId, Box2Rotated attachedBounds, Box2 bounds, Angle worldAngle, out MapCoordinates coords, out Angle angle)
    {
        var magnetPos = _transform.GetWorldPosition(player) + worldAngle.ToVec() * bounds.MaxDimension;
        var origin = magnetPos;
        var fraction = 0.50f;
        var grids = new List<Entity<MapGridComponent>>();

        // Thanks 20kdc
        for (var i = 0; i < 20; i++)
        {
            var randomPos = origin +
                            worldAngle.ToVec() * (rumor.SpawnDistance * fraction) +
                            (worldAngle + Math.PI / 2).ToVec();
            var finalCoords = new MapCoordinates(randomPos, mapId);

            angle = _random.NextAngle();
            var box2 = Box2.CenteredAround(finalCoords.Position, bounds.Size);
            var box2Rot = new Box2Rotated(box2, angle, finalCoords.Position);

            // This doesn't stop it from spawning on top of random things in space
            // Might be better like this, ghosts could stop it before
            grids.Clear();
            _mapSystem.FindGridsIntersecting(finalCoords.MapId, box2Rot, ref grids);
            if (grids.Count > 0)
            {
                // Bump it further and further just in case.
                fraction += 0.1f;
                continue;
            }

            coords = finalCoords;
            return true;
        }

        angle = Angle.Zero;
        coords = MapCoordinates.Nullspace;
        return false;
    }

    public void CancelRumorByIndex(RumorGetterComponent getter, int iD)
    {
        if (getter.Rumors.Count-1 < iD) return;
        getter.Rumors.RemoveAt(iD);
    }

    public void TransferRumorByIndex(RumorGetterComponent getter, int iD, string target, EntityUid player)
    {
        if (target == Name(player)) return;
        if (getter.Rumors.Count - 1 < iD) return;
        var rumor = getter.Rumors[iD];
        var targetGetter = GetRumorGetterByName(target);
        if(targetGetter == null || targetGetter.Rumors.Count > 4)
        {
            NotifyPlayer(player, $"You cannot transfer the rumor to {target} at this time.");
            return;
        }
        else
        {
            var targetImplant = Transform(targetGetter.Owner);
            var targetPlayer = targetImplant.ParentUid;
            if (targetPlayer == null)
            {
                NotifyPlayer(player, $"You cannot transfer the rumor to {target} at this time.");
                return;
            }
            NotifyPlayer(targetPlayer, $"{Name(player)} has transfered a rumor to you.");
            AssignRumor(targetGetter, targetPlayer, null, rumor);
            getter.Rumors.Remove(rumor);
        }
    }
}


