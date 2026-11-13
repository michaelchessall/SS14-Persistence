using Content.Shared.Examine;
using Content.Shared.Gravity;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared._Persistence14.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Upgrades;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._Persistence14.Weapons.Ranged.Systems;

/// <summary>
/// Handles situational spread penalties on guns, applied in this order:
/// <list type="number">
/// <item>Unwielded, see <see cref="GunUnwieldedPenaltyComponent"/>.</item>
/// <item>Zero gravity, for any hand-held gun whose holder is weightless. See <see cref="ZeroGravityGunPenaltyComponent"/> for overrides.</item>
/// </list>
/// Runs after the wield bonus and gun upgrades so the penalties scale the gun's real handling,
/// and before akimbo so akimbo's flat penalty stays flat.
/// </summary>
public sealed partial class GunPenaltySystem : EntitySystem
{
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        // A single handler so the order between penalties is explicit, rather than left to the event bus.
        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnRefreshModifiers,
            after: [typeof(SharedWieldableSystem), typeof(GunUpgradeSystem)]);

        SubscribeLocalEvent<GunUnwieldedPenaltyComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<GunUnwieldedPenaltyComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<GunUnwieldedPenaltyComponent, ExaminedEvent>(OnUnwieldedExamine);

        SubscribeLocalEvent<GunComponent, GotEquippedHandEvent>(OnGotEquipped);
        SubscribeLocalEvent<GunComponent, GotUnequippedHandEvent>(OnGotUnequipped);
        SubscribeLocalEvent<HandsComponent, WeightlessnessChangedEvent>(OnWeightlessnessChanged);
    }

    private void OnRefreshModifiers(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        ApplyUnwieldedPenalty(ent, ref args);
        ApplyZeroGravityPenalty(ent, ref args);
    }

    #region Unwielded

    private void OnWielded(Entity<GunUnwieldedPenaltyComponent> ent, ref ItemWieldedEvent args)
    {
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnUnwielded(Entity<GunUnwieldedPenaltyComponent> ent, ref ItemUnwieldedEvent args)
    {
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnUnwieldedExamine(Entity<GunUnwieldedPenaltyComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<GunRequiresWieldComponent>(ent) || MathHelper.CloseTo(ent.Comp.AngleMultiplier, 1f))
            return;

        if (ent.Comp.ExamineMessage != null)
            args.PushText(Loc.GetString(ent.Comp.ExamineMessage));
    }

    private void ApplyUnwieldedPenalty(EntityUid gun, ref GunRefreshModifiersEvent args)
    {
        if (!TryComp<GunUnwieldedPenaltyComponent>(gun, out var penalty) ||
            TryComp(gun, out WieldableComponent? wield) && wield.Wielded)
        {
            return;
        }

        args.MinAngle *= penalty.AngleMultiplier;
        args.MaxAngle *= penalty.AngleMultiplier;
        args.AngleIncrease *= penalty.AngleMultiplier;
        args.AngleDecay /= penalty.AngleMultiplier;
    }

    #endregion

    #region Zero gravity

    private void OnGotEquipped(Entity<GunComponent> ent, ref GotEquippedHandEvent args)
    {
        _gun.RefreshModifiers(ent.AsNullable());
    }

    private void OnGotUnequipped(Entity<GunComponent> ent, ref GotUnequippedHandEvent args)
    {
        _gun.RefreshModifiers(ent.AsNullable());
    }

    private void OnWeightlessnessChanged(Entity<HandsComponent> ent, ref WeightlessnessChangedEvent args)
    {
        foreach (var held in _hands.EnumerateHeld((ent, ent.Comp)))
        {
            if (TryComp<GunComponent>(held, out var gun))
                _gun.RefreshModifiers((held, gun));
        }
    }

    private void ApplyZeroGravityPenalty(EntityUid gun, ref GunRefreshModifiersEvent args)
    {
        // Only hand-held guns, so turrets and mobs that are guns are unaffected.
        if (Transform(gun).ParentUid is not { Valid: true } user ||
            !_hands.IsHolding(user, gun) ||
            !_gravity.IsWeightless(user))
        {
            return;
        }

        TryComp<ZeroGravityGunPenaltyComponent>(gun, out var penalty);
        var multiplier = penalty?.AngleMultiplier ?? ZeroGravityGunPenaltyComponent.DefaultAngleMultiplier;
        var flat = penalty?.FlatAngle ?? Angle.FromDegrees(ZeroGravityGunPenaltyComponent.DefaultFlatAngleDegrees);
        var max = penalty?.MaxAngle ?? Angle.FromDegrees(ZeroGravityGunPenaltyComponent.DefaultMaxAngleDegrees);

        // Only cap what this penalty adds, never pull an already-wider gun back down.
        var cap = Math.Max(max.Theta, args.MaxAngle.Theta);
        var maxTheta = Math.Min(args.MaxAngle.Theta * multiplier + flat.Theta, cap);
        var minTheta = Math.Min(args.MinAngle.Theta * multiplier + flat.Theta, maxTheta);

        args.MinAngle = new Angle(minTheta);
        args.MaxAngle = new Angle(maxTheta);
        args.AngleIncrease *= multiplier;
        args.AngleDecay /= multiplier;
    }

    #endregion
}
