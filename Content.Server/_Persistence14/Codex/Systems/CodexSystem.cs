using Content.Server.Actions;
using Content.Server.Audio;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.CrewRecords.Systems;
using Content.Server.Database;
using Content.Server.Traits;
using Content.Shared._Persistence14.Codex;
using Content.Shared._Persistence14.Codex.Components;
using Content.Shared._Persistence14.Codex.Prototypes;
using Content.Shared._Persistence14.Rumors.Components;
using Content.Shared._Persistence14.Rumors.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.CrewAssignments;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Emoting;
using Content.Shared.EntityEffects.Effects.Body;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.GridControl.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Content.Server._Persistence14.Codex.Systems;

public sealed partial class CodexSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private CrewMetaRecordsSystem _crewMetaRecords = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private TraitSystem _trait = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;
    [Dependency] private RumorsSystem _rumors = default!;
    [Dependency] private ServerGlobalSoundSystem _globalsound = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private SharedXenoArtifactSystem _artifact = default!;

    private SoundSpecifier _codexUnlockSound = new SoundPathSpecifier("/Audio/Effects/RingtoneNotes/gsharp.ogg");
    private SoundSpecifier _insightUnlockSound = new SoundPathSpecifier("/Audio/Effects/RingtoneNotes/dsharp.ogg");

    [SubscribeLocalEvent]
    private void OnGuideGranterMapInit(Entity<GuideActionGranterComponent> ent, ref MapInitEvent args)
    {
        // try to add scream action when vocal comp added
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        ent.Comp.RemoveIn = ent.Comp.RemoveTime + _timing.CurTime;
    }

    [SubscribeLocalEvent]
    private void OnVoidVision(Entity<ActorComponent> ent, ref VoidVisionsEvent args)
    {
        var getter = _rumors.GetRumorComponent(ent);
        if (getter == null || getter.Value.Comp.Rumors.Count >= 4) return;
        _rumors.AssignRumor(getter.Value, ent);
        _chat.TrySendInGameICMessage(ent.Owner, $"briefly falls into a trance.", InGameICChatType.Emote, true);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnMiracleRevive(Entity<MiracleReviveComponent> ent, ref MiracleReviveEvent args)
    {
        EntityUid target = args.Target;
        EntityUid user = ent.Owner;
        if (TryRevive(ent, user, target)) args.Handled = true;
    }

    private bool TryRevive(Entity<MiracleReviveComponent> ent, EntityUid user, EntityUid target)
    {
        bool failedRevive = true;
        if (_rotting.IsRotten(target))
        {
            _chat.TrySendInGameICMessage(ent.Owner, "cannot revive a rotten corpse.", InGameICChatType.Emote, true);
        }
        else if (TryComp<UnrevivableComponent>(target, out var unrevivable))
        {
            _chat.TrySendInGameICMessage(ent.Owner, "cannot revive them.", InGameICChatType.Emote, true);
        }
        else
        {
            TryComp<MobStateComponent>(target, out var targetMobState);
            if (targetMobState == null || targetMobState.CurrentState != MobState.Dead) return false;
            _damageable.TryChangeDamage(target, ent.Comp.ZapHeal, true, origin: user);
            var canRevive = true;
            if (canRevive && // funky
                TryComp<MobThresholdsComponent>(target, out var targetThresholds) && // funky
                _mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var threshold, targetThresholds) &&
                _damageable.GetTotalDamage(target) < threshold) //is their current health above their death threshold
            {
                _mobState.ChangeMobState(target, MobState.Critical, targetMobState, user); //if so revive them
                failedRevive = false;
                _electrocution.TryDoElectrocution(
                    target,
                    ent.Owner,
                    ent.Comp.ZapDamage,
                    ent.Comp.WritheDuration,
                    true
                );
                _audio.PlayPvs(ent.Comp.ZapSound, ent, null);
                _chat.TrySendInGameICMessage(ent.Owner, $"lays hands on {Name(target)} and zaps them back to life.", InGameICChatType.Emote, true);
            }
        }


        return !failedRevive;
    }

    public float? GetRange(EntityUid start, EntityUid destination)
    {
        var startXform = Transform(start);
        var destinationXform = Transform(destination);
        if (startXform.MapID != destinationXform.MapID) return null;
        var dist = Vector2.Distance(startXform.MapPosition.Position, destinationXform.MapPosition.Position);
        return dist;
    }
    public override void Update(float frameTime)
    {
        var guidebookGranterQuery = EntityQueryEnumerator<GuideActionGranterComponent>();
        while (guidebookGranterQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.Removed) continue;
            if (comp.RemoveIn < _timing.CurTime)
            {
                comp.Removed = true;
                _actions.RemoveAction(uid, comp.ActionEntity);
            }
        }
        var passiveUnlockerQuery = EntityQueryEnumerator<CodexPassiveUnlockerComponent>();
        while (passiveUnlockerQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.EntryToAdd != "" && comp.ToAddRequirement != 0)
            {
                comp.PossibleEntries.Add(comp.EntryToAdd, comp.ToAddRequirement);
                comp.EntryToAdd = "";
                comp.ToAddRequirement = 0;
            }
            if (_timing.CurTime > comp.CurrentCooldown)
            {
                PassiveUnlockerCheck(uid, comp);
            }
        }
        base.Update(frameTime);
    }
    private void PassiveUnlockerCheck(EntityUid uid, CodexPassiveUnlockerComponent comp)
    {
        if (_crewMetaRecords.MetaRecords == null) return;
        comp.CurrentCooldown = _timing.CurTime + comp.CheckCooldown;
        var actorQuery = EntityQueryEnumerator<ActorComponent>();
        while (actorQuery.MoveNext(out var actorEnt, out var actorComp))
        {
            if (Transform(actorEnt).GridUid != uid) continue;
            if (!_crewMetaRecords.MetaRecords.TryGetRecord(Name(actorEnt), out var crewrecord) || crewrecord == null) continue;
            if (_random.Next(0, 2) == 0) continue;
            foreach (var kv in comp.PossibleEntries)
            {
                var entry = kv.Key;
                var progressRequired = kv.Value;
                if (crewrecord.UnlockedEntries.Contains(entry)) continue;
                if (crewrecord.PassiveProgressEntries.ContainsKey(entry))
                {
                    crewrecord.PassiveProgressEntries[entry]++;
                }
                else
                {
                    crewrecord.PassiveProgressEntries[entry] = 1;
                }
                var progress = crewrecord.PassiveProgressEntries[entry];
                if (progress >= progressRequired)
                {
                    crewrecord.PassiveProgressEntries.Remove(entry);
                    UnlockEntry(actorEnt, entry);
                }
                break;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnUnlockerExamined(Entity<CodexExamineUnlockerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Entry == "") return;
        var mob = args.Examiner;
        if (_crewMetaRecords.MetaRecords == null) return;
        if (!_crewMetaRecords.MetaRecords.TryGetRecord(Name(mob), out var crewrecord) || crewrecord == null) return;
        if (!crewrecord.UnlockedEntries.Contains(ent.Comp.Entry))
        {
            UnlockEntry(mob, ent.Comp.Entry);
        }
    }

    [SubscribeLocalEvent]
    private void CodexFragmentAfterInteract(Entity<CodexFragmentComponent> ent, ref UseInHandEvent args)
    {
        var mob = args.User;
        if (_crewMetaRecords.MetaRecords == null) return;
        if (!_crewMetaRecords.MetaRecords.TryGetRecord(Name(mob), out var crewrecord) || crewrecord == null) return;
        foreach (var entry in ent.Comp.PossibleEntries)
        {
            if (crewrecord.UnlockedEntries.Contains(entry)) continue;
            UnlockEntry(mob, entry);
            args.Handled = true;
            QueueDel(ent);
            return;
        }
        NotifyPlayer(mob, "You have learned everything this has to teach.");
    }

    private void OnSenseAnomaly(Entity<ActorComponent> ent, ref SenseAnomalyEvent args)
    {
        EntityUid? closest = null;
        float? distance = null;
        foreach (var arti in _lookup.GetEntitiesInRange<AnomalyComponent>(Transform(ent).Coordinates, 1000))
        {
            if (closest == null || distance == null)
            {
                distance = GetRange(ent, arti);
                closest = arti;
            }
            else
            {
                var range = GetRange(ent, arti);
                if (range != null && range < distance)
                {
                    closest = arti;
                    distance = range;
                }
            }
        }
        if (closest != null)
        {
            var active = Transform(closest.Value);
            NotifyPlayer(ent, $"You sense an anomaly; {Name(closest.Value)} is at {Math.Round(active.MapPosition.Position.X)}, {Math.Round(active.MapPosition.Position.Y)}");
            _chat.TrySendInGameICMessage(ent.Owner, $"looks overwhelmed by a sudden surge of knowledge.", InGameICChatType.Emote, true);

        }
        else
        {
            NotifyPlayer(ent, "There are no anomalies within a 1000m range.");
        }
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    public void OnArtifexiumBurst(Entity<ActorComponent> ent, ref ArtifexiumBurstEvent args)
    {
        bool any = false;
        foreach (var arti in _lookup.GetEntitiesInRange<XenoArtifactComponent>(Transform(ent).Coordinates, args.Range))
        {
            var primedNodes = _artifact.GetPrimedNodes(arti).ToList();
            if (primedNodes.Count == 0)
                continue;

            var node = _random.Pick(primedNodes);

            _artifact.SetNodeUnlocked(arti, node);
            _artifact.ActivateNode(arti, node, ent.Owner, null, Transform(ent).Coordinates, false);
            any = true;
        }

        if (!any)
            NotifyPlayer(ent, "No valid artifacts in range!");
        else
            _audio.PlayPvs(new SoundCollectionSpecifier("ArtifactUnlockingActivationSuccess"), ent.Owner);
        args.Handled = true;
    }

    public void UnlockEntry(EntityUid mob, ProtoId<CodexEntryPrototype> codex)
    {
        if (_crewMetaRecords.MetaRecords == null) return;
        if (!_crewMetaRecords.MetaRecords.TryGetRecord(Name(mob), out var crewrecord) || crewrecord == null) return;
        crewrecord.UnlockedEntries.Add(codex);
        var codexProto = _protoMan.Index(codex);
        var categoryProto = _protoMan.Index(codexProto.Category);
        NotifyPlayer(mob, $"You have unlocked the codex entry '[bold]{codexProto.Name}[/bold]' in the {categoryProto.Name} series.", _codexUnlockSound);
        crewrecord.CodexUnlocksSince++;
        if (crewrecord.CodexUnlocksSince >= 5)
        {
            crewrecord.UnspentInsights++;
            crewrecord.CodexUnlocksSince = 0;
            NotifyPlayer(mob, $"You have enough codex entries to earn an Insight. Spend Insights in the Job Network.", _insightUnlockSound);
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
            if (sound != null)
            {
                _globalsound.PlayLocalSound(sound, actor);
            }
        }
    }

    public void OnInsightPurchase(EntityUid ent, JobNetInsightPurchaseMessage args)
    {
        if (_crewMetaRecords.MetaRecords == null) return;
        if (!_crewMetaRecords.MetaRecords.TryGetRecord(Name(ent), out var crewrecord) || crewrecord == null) return;
        if (crewrecord.UnspentInsights <= 0) return;
        if (crewrecord.UnlockedInsights.Contains(args.Target)) return;
        crewrecord.UnspentInsights--;
        crewrecord.UnlockedInsights.Add(args.Target);
        var insightProto = _protoMan.Index<InsightTraitPrototype>(args.Target);
        var traitProto = _protoMan.Index(insightProto.Trait);
        _trait.AddTrait(ent, traitProto);

    }
}
