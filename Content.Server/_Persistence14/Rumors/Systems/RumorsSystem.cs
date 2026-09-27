using Content.Server._NF.Bank;
using Content.Server.Administration.Managers;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.CrewRecords.Systems;
using Content.Server.Mind;
using Content.Server.Movement.Systems;
using Content.Server.Salvage.Magnet;
using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.Rumors.Components;
using Content.Shared._Persistence14.Rumors.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Atmos.Components;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.Mind;
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
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private PersistentIdentifierSystem _pid = default!;
    [Dependency] private BankSystem _bank = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RumorGetterComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RumorGetterComponent, GridUidChangedEvent>(OnGridChanged);
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
                            if(rumor.Targets.Count == 0)
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
        var bank = _bank.GetMoneyAccountsComponent();
        if (bank == null) return;
        if (bank.TryGetAccount(name, out var account) && account != null)
        {
            account.Balance += rumor.CashReward;
        }

        NotifyPlayer(player, $"You have completed the {rumor.Name} rumor! You have gained {rumor.ReputationReward} reputation with the {rumor.Faction} and ${rumor.CashReward}!", new SoundPathSpecifier("/Audio/Effects/kaching.ogg"));
        ent.Comp.Rumors.Remove(rumor);
    }

    private void OnComponentInit(Entity<RumorGetterComponent> ent, ref ComponentInit args)
    {
        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RumorGetterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            EntityUid? player = null;
            var implant = Transform(uid);
            player = implant.ParentUid;
            if (player == null) return;

            foreach(var rumor in comp.Rumors)
            {

                if(rumor.ShouldSpawn)
                {
                    TrySpawnRumor(player.Value, rumor);
                }
            }

            var timePassed = _timing.CurTime - comp.LastRumorTime;
            if(timePassed >= comp.NextRumor)
            {
                comp.LastRumorTime = _timing.CurTime;
                AssignRumor(comp, player.Value);
            }
        }
        base.Update(frameTime);
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
    public void AssignRumor(RumorGetterComponent comp, EntityUid player, ProtoId<MetaFactionPrototype>? factionId = null)
    {
        // Time to give a new rumor
        var newRumor = GenerateRumor(player, comp, factionId);
        if (newRumor != null)
        {
            comp.Rumors.Add(newRumor);
            NotifyPlayer(player, $"Your job network reports you have recieved a new rumor: {newRumor.Name}");
        }
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
        }
        if (sound != null)
        {
            _audio.PlayEntity(sound, player, player);
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
        ActiveRumor final = new();
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
        final.CashReward = rumorProto.CashReward;
        final.ReputationReward = rumorProto.ReputationReward;
        final.Description = RealizeDescription(rumorProto, chosenFaction, final);
        return final;
        
    }

    private string RealizeDescription(RumorPrototype rumor, MetaFactionPrototype chosenFaction, ActiveRumor active)
    {
        if(rumor.CompletionType == CompletionType.Discover)
        {
            if (active.TargetPosition == null) return rumor.Description;
            string addon = "\n";
            if(active.GridsToSpawn > 1)
            {
                addon += $"{active.GridsToSpawn} grids will arrive as you approach\n[color=yellow]{Math.Round(active.TargetPosition.Value.Position.X)}, {Math.Round(active.TargetPosition.Value.Position.Y)})[/color]\nExplore them all.";
            }
            else
            {
                addon += $"A grid will arrive as you approach\n[color=yellow]({Math.Round(active.TargetPosition.Value.Position.X)}, {Math.Round(active.TargetPosition.Value.Position.Y)})[/color]\nExplore it!";
            }
            return rumor.Description + addon;
        }
        return rumor.Description;
    }

    private void SpawnEventGrids(ActiveRumor rumor, EntityUid player)
    {
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

            var salvXForm = Transform(mapChild);
            var localPos = salvXForm.LocalPosition;

            _transform.SetParent(mapChild, salvXForm, spawnUid.Value);
            _transform.SetWorldPositionRotation(mapChild, spawnLocation.Position + localPos, spawnAngle, salvXForm);

            // Handle mob restrictions
            var children = salvXForm.ChildEnumerator;

            while (children.MoveNext(out var child))
            {
                if (!_salvMobQuery.TryGetComponent(child, out var salvMob))
                    continue;

                salvMob.LinkedEntity = mapChild;
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
}


