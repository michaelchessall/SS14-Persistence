using Content.Server._NF.Bank;
using Content.Server._Persistence14.Codex.Systems;
using Content.Server.CrewRecords.Systems;
using Content.Server.NPC.Queries.Queries;
using Content.Server.Storage.EntitySystems;
using Content.Server.Traits;
using Content.Shared._Persistence14.Background.Prototypes;
using Content.Shared._Persistence14.Rumors.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Species;
using Content.Shared.Storage;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using NetCord;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Background;

public sealed partial class CharacterBackgroundSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private TraitSystem _traitSystem = default!;
    [Dependency] private CrewMetaRecordsSystem _crewMetaRecords = default!;
    [Dependency] private RumorsSystem _rumors = default!;
    [Dependency] private CodexSystem _codex = default!;
    [Dependency] private BankSystem _bank = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private StorageSystem _storage = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }


    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {

        if (_crewMetaRecords.MetaRecords == null) return;

        _crewMetaRecords.MetaRecords.TryGetRecord(Name(args.Mob), out var crewrecord);



        var profile = args.Profile;
        var mob = args.Mob;
        List<ProtoId<BackgroundEffectPrototype>> effects = new();
        if (profile.Alignment != null)
        {
            var alignment = ProtoMan.Index<AlignmentPrototype>(profile.Alignment);
            effects.AddRange(alignment.Effects);
        }
        if (profile.UniverseOrigin != null)
        {
            var alignment = ProtoMan.Index<UniverseOriginPrototype>(profile.UniverseOrigin);
            effects.AddRange(alignment.Effects);
        }
        if (profile.Motive != null)
        {
            var alignment = ProtoMan.Index<MotivePrototype>(profile.Motive);
            effects.AddRange(alignment.Effects);
        }
        if (profile.TurningPoint != null)
        {
            var alignment = ProtoMan.Index<TurningPointPrototype>(profile.TurningPoint);
            effects.AddRange(alignment.Effects);
        }
        foreach (var effect in effects)
        {
            if (!ProtoMan.Resolve(effect, out var effectProto))
                continue;
            foreach (var kv in effectProto.Equipment)
            {
                var itemType = kv.Value;
                if (_inventorySystem.TryUnequip(mob, kv.Key, out var unequippedItem, silent: true, force: true, reparent: false, skipChildren: true))
                {
                    if (unequippedItem != null)
                    {
                        QueueDel(unequippedItem.Value);
                    }
                }
                if (itemType != string.Empty)
                {
                    var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                    _inventorySystem.TryEquip(mob, item, kv.Key, true, true);
                }
            }
            if (effectProto.Storage.Count > 0)
            {
                var entity = mob;
                var coords = _xform.GetMapCoordinates(entity);
                _inventoryQuery.TryComp(entity, out var inventoryComp);

                foreach (var (slotName, entProtos) in effectProto.Storage)
                {
                    if (entProtos == null || entProtos.Count == 0)
                        continue;

                    if (inventoryComp != null &&
                        _inventorySystem.TryGetSlotEntity(entity, slotName, out var slotEnt, inventoryComponent: inventoryComp) &&
                        _storageQuery.TryComp(slotEnt, out var storage))
                    {

                        foreach (var entProto in entProtos)
                        {
                            var spawnedEntity = Spawn(entProto, coords);

                            _storage.Insert(slotEnt.Value, spawnedEntity, out _, storageComp: storage, playSound: false);
                        }
                    }
                }
            }
            foreach (var trait in effectProto.Traits)
            {
                var traitProto = ProtoMan.Index<TraitPrototype>(trait);
                _traitSystem.AddTrait(mob, traitProto);
            }
            if (crewrecord != null)
            {
                foreach (var kv in effectProto.ReputationGains)
                {
                    var total = kv.Value;
                    if (crewrecord.MetaFactionReputations.ContainsKey(kv.Key))
                    {
                        total += crewrecord.MetaFactionReputations[kv.Key];
                    }
                    crewrecord.MetaFactionReputations[kv.Key] = total;
                }
                if (effectProto.BonusCash > 0)
                {
                    _bank.TryBankDeposit(args.Mob, effectProto.BonusCash);
                }
            }
            var rumorEnt = _rumors.GetRumorComponent(args.Mob);
            if (rumorEnt != null)
            {
                if (effectProto.ExtraRumors > 0)
                {

                    for (int i = 1; i <= effectProto.ExtraRumors; i++)
                    {
                        _rumors.AssignRumor(rumorEnt, args.Mob);
                    }

                }
                foreach (var kv in effectProto.ExtraFactionRumors)
                {
                    for (int i = 1; i <= kv.Value; i++)
                    {
                        _rumors.AssignRumor(rumorEnt, args.Mob, kv.Key);
                    }
                }
            }
            foreach (var codex in effectProto.CodexEntries)
            {
                _codex.UnlockEntry(mob, codex);
            }
        }
    }
}
