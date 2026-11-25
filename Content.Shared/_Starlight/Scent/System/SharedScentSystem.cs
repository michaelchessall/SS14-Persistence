using Content.Shared._Starlight.Scent.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Scent.Systems;

// See ClientScentSystem for the concrete client-side subclass.
public abstract class SharedScentSystem : EntitySystem
{
    [Dependency] protected SharedActionsSystem Actions = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected MobStateSystem MobState = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmellerComponent, MapInitEvent>(OnSmellerMapInit);
        SubscribeLocalEvent<SmellerComponent, ComponentShutdown>(OnSmellerShutdown);
        SubscribeLocalEvent<SmellerComponent, ToggleSniffActionEvent>(OnToggleSniff);
        SubscribeLocalEvent<SmellerComponent, SneezeActionEvent>(OnSneeze);
        SubscribeLocalEvent<SmellerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<TrackingScentStatusEffectComponent, StatusEffectRemovedEvent>(OnTrackingStatusEffectEnded);
    }

    private void OnMobStateChanged(Entity<SmellerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == Content.Shared.Mobs.MobState.Dead)
            ClearTrackedScent(ent);
    }

    private void OnSmellerMapInit(Entity<SmellerComponent> ent, ref MapInitEvent args)
    {
        Actions.AddAction(ent.Owner, ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
    }

    // virtual so ClientScentSystem can override instead of subscribing to ComponentShutdown again.
    protected virtual void OnSmellerShutdown(Entity<SmellerComponent> ent, ref ComponentShutdown args)
    {
        Actions.RemoveAction(ent.Owner, ent.Comp.ToggleActionEntity);
        Actions.RemoveAction(ent.Owner, ent.Comp.SneezeActionEntity);
        Actions.RemoveAction(ent.Owner, ent.Comp.SniffObjectActionEntity);
        _statusEffects.TryRemoveStatusEffect(ent.Owner, ent.Comp.TrackStatusEffect);
    }

    private void OnToggleSniff(Entity<SmellerComponent> ent, ref ToggleSniffActionEvent args)
    {
        if (args.Handled)
            return;

        SetSniffing(ent, !ent.Comp.Sniffing);
        args.Handled = true;
    }

    private void OnSneeze(Entity<SmellerComponent> ent, ref SneezeActionEvent args)
    {
        if (args.Handled)
            return;

        Sneeze(ent, predicted: true);
        args.Handled = true;
    }

    // predicted excludes the performer from the server's broadcast; ForceSneeze doesn't want that.
    public void Sneeze(Entity<SmellerComponent> ent, bool predicted = false)
    {
        ClearTrackedScent(ent, predicted ? ScentTrackingStoppedReason.Sneeze : ScentTrackingStoppedReason.ForceSneeze);
        Audio.PlayPredicted(ent.Comp.SneezeSound, ent.Owner, predicted ? ent.Owner : null);

        var message = Loc.GetString("scent-sneeze-popup");
        if (predicted)
            Popup.PopupClient(message, ent.Owner, ent.Owner);
        else
            Popup.PopupEntity(message, ent.Owner, ent.Owner);
    }

    // Not the voluntary sneeze action. Locks out Toggle Smelling for 'lockout'.
    public void ForceSneeze(Entity<SmellerComponent> ent, TimeSpan lockout)
    {
        if (MobState.IsDead(ent.Owner))
            return;

        if (TryComp<ActionComponent>(ent.Comp.ToggleActionEntity, out var toggleAction) &&
            Actions.IsCooldownActive(toggleAction))
        {
            Actions.SetCooldown(ent.Comp.ToggleActionEntity, lockout);
            return;
        }

        Sneeze(ent);
        SetSniffing(ent, false);
        Actions.SetCooldown(ent.Comp.ToggleActionEntity, lockout);
    }
    /// <summary>
    /// Starts (or switches) tracking a scent. Grants the sneeze action if not already tracking,
    /// and (re)starts the TrackStatusEffect timer.
    /// </summary>
    /// <param name="ent">Entity with a Smeller component.</param>
    /// <param name="scentId">The ScentId to track.</param>
    /// <param name="source">The entity that was sniffed to identify this scentId, if any - used only for admin logging.</param>

    public void SetTrackedScent(Entity<SmellerComponent> ent, string scentId, EntityUid? source = null)
    {
        if (ent.Comp.TrackedScentId == scentId)
            return;

        var previousScentId = ent.Comp.TrackedScentId;
        var hadTracked = previousScentId != null;
        ent.Comp.TrackedScentId = scentId;

        if (!hadTracked)
            Actions.AddAction(ent.Owner, ref ent.Comp.SneezeActionEntity, ent.Comp.SneezeAction);

        if (previousScentId != null)
            LogTrackedScent(ent.Owner, previousScentId, began: false);

        LogTrackedScent(ent.Owner, scentId, began: true, source);

        _statusEffects.TrySetStatusEffectDuration(ent.Owner, ent.Comp.TrackStatusEffect, ent.Comp.TrackDuration);

        Dirty(ent);
    }

    /// <summary>
    /// Stops tracking the current scent, if any. Removes the sneeze action, removes the
    /// TrackStatusEffect, and logs the stop with <paramref name="reason"/> if given.
    /// </summary>
    /// <param name="ent">Entity with a Smeller component.</param>
    /// <param name="reason">Why tracking stopped - timeout, sneeze, or forced sneeze. Left null
    /// for other cases (death, re-track, component removal), which log with no reason suffix.</param>
    public void ClearTrackedScent(Entity<SmellerComponent> ent, ScentTrackingStoppedReason? reason = null)
    {
        if (ent.Comp.TrackedScentId is not { } scentId)
            return;

        ent.Comp.TrackedScentId = null;
        Actions.RemoveAction(ent.Owner, ent.Comp.SneezeActionEntity);

        _statusEffects.TryRemoveStatusEffect(ent.Owner, ent.Comp.TrackStatusEffect);

        LogTrackedScent(ent.Owner, scentId, began: false, reason: reason);

        Dirty(ent);
    }

    private void OnTrackingStatusEffectEnded(Entity<TrackingScentStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp<SmellerComponent>(args.Target, out var smellerComp) || smellerComp.TrackedScentId == null)
            return;

        var timedOut = TryComp<StatusEffectComponent>(ent.Owner, out var effect) &&
            effect.EndEffectTime is { } endTime && _timing.CurTime >= endTime;

        ClearTrackedScent((args.Target, smellerComp), timedOut ? ScentTrackingStoppedReason.Timeout : null);

        if (timedOut)
            Popup.PopupEntity(Loc.GetString("scent-track-fades-popup"), args.Target, args.Target);
    }

    private void LogTrackedScent(EntityUid smeller, string scentId, bool began, EntityUid? source = null, ScentTrackingStoppedReason? reason = null)
    {
        var verb = began ? "began" : "stopped";
        var hasOwner = TryResolveScentOwner(scentId, out var owner);
        var suffix = reason switch
        {
            ScentTrackingStoppedReason.Timeout => " (timed out)",
            ScentTrackingStoppedReason.Sneeze => " (sneezed)",
            ScentTrackingStoppedReason.ForceSneeze => " (forced sneeze)",
            _ => "",
        };

        if (source is { } src)
        {
            if (hasOwner)
                _adminLogger.Add(LogType.Scent,
                    $"{ToPrettyString(smeller):user} sniffed {ToPrettyString(src):source} and {verb} following scent trace belonging to {ToPrettyString(owner):target}{suffix}.");
            else
                _adminLogger.Add(LogType.Scent,
                    $"{ToPrettyString(smeller):user} sniffed {ToPrettyString(src):source} and {verb} following an untraceable scent trace{suffix}.");
        }
        else if (hasOwner)
        {
            _adminLogger.Add(LogType.Scent,
                $"{ToPrettyString(smeller):user} {verb} following scent trace belonging to {ToPrettyString(owner):target}{suffix}.");
        }
        else
        {
            _adminLogger.Add(LogType.Scent, $"{ToPrettyString(smeller):user} {verb} following an untraceable scent trace{suffix}.");
        }
    }

    private bool TryResolveScentOwner(string scentId, out EntityUid owner)
    {
        var query = EntityQueryEnumerator<ScentComponent>();
        while (query.MoveNext(out var uid, out var scent))
        {
            if (scent.ScentId == scentId)
            {
                owner = uid;
                return true;
            }
        }

        owner = default;
        return false;
    }

    // Sneeze action grant/revoke happens in SetTrackedScent/ClearTrackedScent instead.
    public virtual void SetSniffing(Entity<SmellerComponent> ent, bool sniffing)
    {
        if (ent.Comp.Sniffing == sniffing)
            return;

        ent.Comp.Sniffing = sniffing;
        Actions.SetToggled(ent.Comp.ToggleActionEntity, sniffing);

        if (sniffing)
            Actions.AddAction(ent.Owner, ref ent.Comp.SniffObjectActionEntity, ent.Comp.SniffObjectAction);
        else
            Actions.RemoveAction(ent.Owner, ent.Comp.SniffObjectActionEntity);

        // The generic action-use log already covers Action; this adds the on/off detail under Scent.
        var state = sniffing ? "on" : "off";
        _adminLogger.Add(LogType.Scent, $"{ToPrettyString(ent.Owner):user} toggled sniffing {state}.");


        Dirty(ent);
    }

    public virtual void RandomizeScent(Entity<ScentComponent?> ent) { }
}

public enum ScentTrackingStoppedReason
{
    Timeout,
    Sneeze,
    ForceSneeze,
}
