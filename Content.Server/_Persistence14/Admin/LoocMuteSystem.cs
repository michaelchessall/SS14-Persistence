using Content.Server._Persistence14.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._Persistence14.Admin;

public sealed partial class LoocMuteSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    private Entity<LoocMuteManagerComponent>? _manager = null;

    [SubscribeLocalEvent]
    private void OnPostInit(ref PostInitEvent args)
    {
        if (TryGetManager(out var manager))
        {
            _manager = manager;
            return;
        }

        var query = EntityQueryEnumerator<TransformComponent>();
        while (query.MoveNext(out _, out var xform))
        {
            if (xform.MapUid is not { } mapId)
                continue;

            var comp = EnsureComp<LoocMuteManagerComponent>(mapId);
            _manager = (mapId, comp);
            return;
        }

        LogManager.GetSawmill("looc-mute-system").Error($"Unable to find valid LoocMuteManager target.");
    }

    /// <summary>
    /// Verifies a player's muted status. If their duration has expired, removes them from the list of durations.
    /// </summary>
    public bool IsPlayerMuted(NetUserId user, bool modifyState = true)
    {
        if (!TryGetManager(out var manager))
        {
            LogManager.GetSawmill("looc-mute-system").Error("No valid LoocMuteManager found, unable to enforce muting.");
            return false;
        }

        if (manager.Comp.PermanentMuteIds.Contains(user))
            return true; // Perma muted

        if (!manager.Comp.MuteDurations.TryGetValue(user, out var duration))
            return false; // Not known muted

        // Duration has expired! Yay!
        if (duration <= _timing.CurTime)
        {
            if (modifyState) manager.Comp.MuteDurations.Remove(user);
            return false;
        }

        return true; // Their duration has yet to expire.
    }

    /// <summary>
    /// Mutes a player for a specified duration. If no duration is set, mute is permanent until removed.
    /// </summary>
    public void MutePlayer(NetUserId userId, TimeSpan? duration = null)
    {
        if (!TryGetManager(out var manager))
        {
            LogManager.GetSawmill("looc-mute-system").Error($"Attempted to mute player but LoocMuteManager not found. Please add to the map entity and try again.");
            return;
        }

        if (duration is null)
        {
            manager.Comp.PermanentMuteIds.Add(userId);
            return; // If youre perma muted, doesn't really matter that you are time muted does it...
        }

        var expiresAt = manager.Comp.MuteDurations.TryGetValue(userId, out var stored) ? stored.Duration : TimeSpan.Zero;
        var start = expiresAt > _timing.CurTime ? expiresAt : _timing.CurTime;
        manager.Comp.MuteDurations[userId] = duration.Value + start;
    }

    /// <summary>
    /// Unmutes a player by removing them from both the permanent and duration lists, if present.
    /// </summary>
    public void UnmutePlayer(NetUserId userId)
    {
        if (!TryGetManager(out var manager))
        {
            LogManager.GetSawmill("looc-mut-system").Error($"Attempted to unmute player but LoocMuteManager not found. Please add to the map entity and try again.");
            return;
        }

        manager.Comp.PermanentMuteIds.Remove(userId);
        manager.Comp.MuteDurations.Remove(userId);
    }

    /// <summary>
    /// Gets the manager from storage, or finds it, provided it exists.
    /// </summary>
    private bool TryGetManager(out Entity<LoocMuteManagerComponent> manager)
    {
        if (_manager is not null)
        {
            manager = _manager.Value;
            return true;
        }

        var query = EntityQueryEnumerator<LoocMuteManagerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            manager = (uid, comp);
            return true;
        }
        manager = default!;
        return false;
    }
}