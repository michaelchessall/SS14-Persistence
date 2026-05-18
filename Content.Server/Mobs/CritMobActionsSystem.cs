using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Shared.AcceptDeath;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Speech.Muting;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Mobs;

/// <summary>
///     Handles performing crit-specific actions.
/// </summary>
public sealed class CritMobActionsSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DeathgaspSystem _deathgasp = default!;
    [Dependency] private IServerConsoleHost _host = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IServerPreferencesManager _prefsManager = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IConfigurationManager _configurationManager = default!;
    private const int MaxLastWordsLength = 30;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateActionsComponent, CritSuccumbEvent>(OnSuccumb);
        SubscribeLocalEvent<MobStateActionsComponent, CritFakeDeathEvent>(OnFakeDeath);
        SubscribeLocalEvent<MobStateActionsComponent, CritLastWordsEvent>(OnLastWords);
        SubscribeLocalEvent<MobStateActionsComponent, AcceptDeathEvent>(OnAcceptDeath);
        SubscribeLocalEvent<MobStateActionsComponent, AcceptDeathFinalizeMessage>(FinalizeAcceptDeath);
        SubscribeLocalEvent<MobStateActionsComponent, AcceptDeathSOSMessage>(TriggerSOS);
    }



    private void OnSuccumb(EntityUid uid, MobStateActionsComponent component, CritSuccumbEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor) || !_mobState.IsCritical(uid))
            return;

        _host.ExecuteCommand(actor.PlayerSession, "ghost");
        args.Handled = true;
    }

    private void OnFakeDeath(EntityUid uid, MobStateActionsComponent component, CritFakeDeathEvent args)
    {
        if (!_mobState.IsCritical(uid))
            return;

        if (HasComp<MutedComponent>(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("fake-death-muted"), uid, uid);
            return;
        }

        args.Handled = _deathgasp.Deathgasp(uid);
    }

    private void OnLastWords(EntityUid uid, MobStateActionsComponent component, CritLastWordsEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("action-name-crit-last-words"), "",
            (string lastWords) =>
            {
                // if a person is gibbed/deleted, they can't say last words
                if (Deleted(uid))
                    return;

                // Intentionally does not check for muteness
                if (actor.PlayerSession.AttachedEntity != uid
                    || !_mobState.IsCritical(uid))
                    return;

                if (lastWords.Length > MaxLastWordsLength)
                {
                    lastWords = lastWords.Substring(0, MaxLastWordsLength);
                }
                lastWords += "...";

                _chat.TrySendInGameICMessage(uid, lastWords, InGameICChatType.Whisper, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);
                _host.ExecuteCommand(actor.PlayerSession, "ghost");
            });

        args.Handled = true;
    }

    public void ToggleUi(EntityUid user, EntityUid jobnetEnt, MobStateActionsComponent? component = null)
    {
        if (!Resolve(user, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!_ui.TryToggleUi(user, AcceptDeathUiKey.Key, actor.PlayerSession))
            return;

        UpdateUserInterface(user, jobnetEnt, component);
    }
    public void UpdateUserInterface(EntityUid? user, EntityUid jobnet, MobStateActionsComponent? component = null)
    {
        if (!Resolve(jobnet, ref component) || user == null || component == null)
            return;

        var state = new AcceptDeathUpdateState(component.AcceptDeathCooldown - _timing.CurTime, component.SOSCooldown - _timing.CurTime);
        _ui.SetUiState(jobnet, AcceptDeathUiKey.Key, state);
    }
    private void OnAcceptDeath(EntityUid uid, MobStateActionsComponent component, AcceptDeathEvent args)
    {
        ToggleUi(args.Performer, uid, component);
    }

    public bool ValidateAcceptDeath(EntityUid uid, MobStateActionsComponent component)
    {
        if (component.AcceptDeathCooldown > _timing.CurTime) return false;
        TryComp<MobStateComponent>(uid, out var state);
        if (state == null) return false;
        if (state.CurrentState != MobState.Dead) return false;
        return true;
    }
    public bool ValidateSOS(EntityUid uid, MobStateActionsComponent component)
    {
        if (component.SOSCooldown > _timing.CurTime) return false;
        TryComp<MobStateComponent>(uid, out var state);
        if (state == null) return false;
        if (state.CurrentState != MobState.Dead) return false;
        return true;
    }

    private void TriggerSOS(EntityUid uid, MobStateActionsComponent component, AcceptDeathSOSMessage args)
    {
        if (!ValidateSOS(uid, component))
        {
            return;
        }
        var xform = Transform(uid);
        var mapPos = _transform.GetWorldPosition(xform);
        _radio.SendRadioMessage(uid, $"{Name(uid)} has died at ({mapPos.X:F1}, {mapPos.Y:F1}) and is broadcasting an SOS.", "Common", uid, true, false);
        var respawnTime = TimeSpan.FromSeconds(_configurationManager.GetCVar(CCVars.AcceptDeathTime));
        component.SOSCooldown = _timing.CurTime + respawnTime;
        UpdateUserInterface(uid, uid, component);
    }

    private void FinalizeAcceptDeath(EntityUid uid, MobStateActionsComponent component, AcceptDeathFinalizeMessage args)
    {
        if (!ValidateAcceptDeath(uid, component))
        {
            return;
        }

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _quickDialog.OpenDialog(actor.PlayerSession,
            "Accept Death",
            "Give up on your character being revived and return to the Lobby to make a new character. Your character will be permanently deleted.",
            (string lastWords) =>
            {
                TryComp<MobStateComponent>(uid, out var state);
                if (state != null && !_mobState.IsDead(uid))
                    return;

                if (actor.PlayerSession.AttachedEntity != uid)
                    return;
                if (!ValidateAcceptDeath(uid, component))
                {
                    return;
                }
                var foundSlot = 0;
                PlayerPreferences playerPrefs = _prefsManager.GetPreferences(actor.PlayerSession.UserId);
                var mind = actor.PlayerSession.GetMind();
                string charName = "";
                if (TryComp<MindComponent>(mind, out var mindComp))
                {
                    var name = mindComp.CharacterName;
                    if (name != null)
                    {
                        charName = name;
                    }
                }

                foreach (var pair in playerPrefs.Characters)
                {
                    var profile = pair.Value;
                    if (profile.Name == charName)
                    {
                        foundSlot = pair.Key;
                    }
                }
                _prefsManager.DeleteCharacter(foundSlot, actor.PlayerSession.UserId, actor.PlayerSession);
                _ticker.Respawn(actor.PlayerSession);
            });

    }
}
