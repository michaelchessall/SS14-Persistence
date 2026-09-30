using Content.Server.CrewRecords.Systems;
using Content.Server.Traits;
using Content.Shared._Persistence14.Background.Prototypes;
using Content.Shared._Persistence14.Rumors.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
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
            foreach (var trait in effectProto.Traits)
            {
                var traitProto = ProtoMan.Index<TraitPrototype>(trait);
                _traitSystem.AddTrait(mob, traitProto);
            }
            if(crewrecord != null)
            {
                foreach (var kv in effectProto.ReputationGains)
                {
                    var total = kv.Value;
                    if(crewrecord.MetaFactionReputations.ContainsKey(kv.Key))
                    {
                        total += crewrecord.MetaFactionReputations[kv.Key];
                    }
                    crewrecord.MetaFactionReputations[kv.Key] = total;
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
        }
    }
}
