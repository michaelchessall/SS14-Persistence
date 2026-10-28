using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared._Persistence14.Weapons.Ranged.Components;
using Content.Shared._Persistence14.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Weapons.Ranged.Systems;

/// <summary>
/// Lets a user holding an akimbo-capable gun in each hand fire both, via a toggleable action.
/// Usually the guns alternate with each trigger pull, by redirecting which gun <see cref="SharedGunSystem.TryGetGun"/> returns,
/// so each gun keeps its own fire rate, fire mode and ammo.
/// If both guns are set to full-auto, they fire together instead: the gun being shot also fires its partner.
/// Either way, both guns get a flat spread penalty while akimbo is on.
/// </summary>
public sealed partial class SharedAkimboSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private static readonly EntProtoId ActionId = "ActionToggleAkimbo";

    /// <summary>
    /// The partner gun currently being fired alongside the leading gun, so its own shot doesn't fire the leader back.
    /// </summary>
    private EntityUid? _firingPartner;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>(OnEquip);
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>(OnUnequip);
        SubscribeLocalEvent<AkimboCapableComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<AkimboCapableComponent, OnEmptyGunShotEvent>(OnEmptyGunShot);
        SubscribeLocalEvent<AkimboCapableComponent, GunRefreshModifiersEvent>(OnRefreshModifiers,
            after: [typeof(GunPenaltySystem)]);
        SubscribeLocalEvent<AkimboUserComponent, GetUserGunEvent>(OnGetUserGun);
        SubscribeLocalEvent<AkimboUserComponent, ToggleAkimboActionEvent>(OnToggle);
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);
    }

    private void OnEquip(Entity<HandsComponent> ent, ref DidEquipHandEvent args)
    {
        Refresh(ent);
        RefreshHeldGuns(ent);
    }

    private void OnUnequip(Entity<HandsComponent> ent, ref DidUnequipHandEvent args)
    {
        Refresh(ent);
        RefreshHeldGuns(ent);

        // The dropped gun is no longer held, so this clears its penalty.
        if (HasComp<AkimboCapableComponent>(args.Unequipped))
            _gun.RefreshModifiers(args.Unequipped);
    }

    private void RefreshHeldGuns(EntityUid user)
    {
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (HasComp<AkimboCapableComponent>(held))
                _gun.RefreshModifiers(held);
        }
    }

    private void Refresh(Entity<HandsComponent> user)
    {
        var count = 0;
        foreach (var held in _hands.EnumerateHeld((user, user.Comp)))
        {
            if (HasComp<AkimboCapableComponent>(held))
                count++;
        }

        if (count >= 2)
        {
            var comp = EnsureComp<AkimboUserComponent>(user);
            if (_actions.GetAction(comp.Action) is { } current && current.Comp.AttachedEntity == user.Owner)
                return;

            if (_actions.AddAction(user, ref comp.Action, ActionId))
                Dirty(user, comp);
        }
        else if (TryComp<AkimboUserComponent>(user, out var existing) &&
                 _actions.GetAction(existing.Action) is { } action &&
                 action.Comp.AttachedEntity == user.Owner)
        {
            _actions.SetToggled((action, action), false);
            _actions.RemoveAction(user.Owner, (action, action));
        }
    }

    private void OnToggle(Entity<AkimboUserComponent> ent, ref ToggleAkimboActionEvent args)
    {
        if (args.Handled)
            return;

        // Toggle here rather than via args.Toggle, which only applies after this handler,
        // so the refreshed spread sees the new state.
        _actions.SetToggled(args.Action.AsNullable(), !args.Action.Comp.Toggled);
        RefreshHeldGuns(ent);
        args.Handled = true;
    }

    private void OnRefreshModifiers(Entity<AkimboCapableComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (Transform(ent).ParentUid is not { Valid: true } user ||
            !_hands.IsHolding(user, ent.Owner) ||
            !TryComp<AkimboUserComponent>(user, out var akimbo) ||
            !IsToggled(akimbo) ||
            !TryGetPartner(user, ent, out _))
        {
            return;
        }

        args.MinAngle += ent.Comp.SpreadPenalty;
        args.MaxAngle += ent.Comp.SpreadPenalty;
    }

    private void OnGunShot(Entity<AkimboCapableComponent> ent, ref GunShotEvent args)
    {
        SetLastFired(args.User, ent);

        if (ent.Owner != _firingPartner)
            TryFirePartner(args.User, ent);
    }

    private void OnEmptyGunShot(Entity<AkimboCapableComponent> ent, ref OnEmptyGunShotEvent args)
    {
        // Count dry fires too, otherwise an empty gun would keep getting picked and the loaded one never would.
        SetLastFired(args.User, ent);
    }

    private void SetLastFired(EntityUid user, EntityUid gun)
    {
        if (!TryComp<AkimboUserComponent>(user, out var akimbo) || akimbo.LastFired == gun)
            return;

        akimbo.LastFired = gun;
        Dirty(user, akimbo);
    }

    private void TryFirePartner(EntityUid user, EntityUid leader)
    {
        if (!TryComp<AkimboUserComponent>(user, out var akimbo) ||
            !IsToggled(akimbo) ||
            !TryComp<GunComponent>(leader, out var leaderGun) ||
            leaderGun.ShootCoordinates is not { } coordinates ||
            !TryGetPartner(user, leader, out var partner) ||
            !IsFullAutoPair(leaderGun, partner.Value.Comp) ||
            _gun.GetAmmoCount(partner.Value) == 0)
        {
            return;
        }

        _firingPartner = partner.Value.Owner;
        try
        {
            // Keep the partner's shot counter while the trigger is held, like the leader's, so a late shoot request
            // lets it catch up on missed shots instead of firing fewer than the client predicted.
            // It gets reset in OnStopShootRequest.
            _gun.AttemptShoot(user, partner.Value, coordinates, leaderGun.Target, resetShotCounter: false);
        }
        finally
        {
            _firingPartner = null;
        }
    }

    private void OnStopShootRequest(RequestStopShootEvent ev, EntitySessionEventArgs args)
    {
        // The stop request only names the leading gun, so release the partner too.
        if (args.SenderSession.AttachedEntity is not { } user ||
            !HasComp<AkimboUserComponent>(user) ||
            !TryGetPartner(user, GetEntity(ev.Gun), out var partner))
        {
            return;
        }

        _gun.StopShooting(partner.Value);
    }

    private void OnGetUserGun(Entity<AkimboUserComponent> ent, ref GetUserGunEvent args)
    {
        if (!IsToggled(ent.Comp) ||
            !HasComp<AkimboCapableComponent>(args.Gun) ||
            !TryComp<GunComponent>(args.Gun, out var activeGun) ||
            !TryGetPartner(ent.Owner, args.Gun, out var partner))
        {
            return;
        }

        // Full-auto pair: the active gun leads and fires its partner alongside it (see TryFirePartner).
        // If the active gun is empty, let the partner lead so holding the trigger keeps firing.
        if (IsFullAutoPair(activeGun, partner.Value.Comp))
        {
            if (_gun.GetAmmoCount(args.Gun) == 0 && _gun.GetAmmoCount(partner.Value) > 0)
                args.Gun = partner.Value;

            return;
        }

        if (ent.Comp.LastFired is not { } last || !_hands.IsHolding(ent.Owner, last))
            return;

        // Trigger is still held (or a burst is still going) on the last gun, so keep using it.
        // This also makes sure the stop-shooting request reaches the gun that needs its shot counter reset.
        if (TryComp<GunComponent>(last, out var lastGun) && (lastGun.ShotCounter > 0 || lastGun.BurstActivated))
        {
            args.Gun = last;
            return;
        }

        // Trigger was released, so the next pull goes to the other akimbo gun.
        if (TryGetPartner(ent.Owner, last, out var next))
            args.Gun = next.Value;
    }

    private bool IsToggled(AkimboUserComponent akimbo)
    {
        return _actions.GetAction(akimbo.Action) is { } action && action.Comp.Toggled;
    }

    private static bool IsFullAutoPair(GunComponent a, GunComponent b)
    {
        return a.SelectedMode == SelectiveFire.FullAuto && b.SelectedMode == SelectiveFire.FullAuto;
    }

    /// <summary>
    /// Finds the other akimbo-capable gun the user is holding besides <paramref name="gun"/>.
    /// </summary>
    private bool TryGetPartner(EntityUid user, EntityUid gun, [NotNullWhen(true)] out Entity<GunComponent>? partner)
    {
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (held != gun && HasComp<AkimboCapableComponent>(held) && TryComp<GunComponent>(held, out var heldGun))
            {
                partner = (held, heldGun);
                return true;
            }
        }

        partner = null;
        return false;
    }
}

public sealed partial class ToggleAkimboActionEvent : InstantActionEvent;
