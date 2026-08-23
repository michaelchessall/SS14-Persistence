using Content.Shared.Gravity;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._Persistence14.Antigravity;

/// <summary>
/// A version of <see cref="Content.Shared.Clothing.EntitySystems.AntiGravityClothingSystem"/> t
/// hat works on the root entity instead of clothing.
/// 
/// Does not depend on standing status.
/// </summary>
public sealed partial class AntiGravitySystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AntiGravityComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AntiGravityComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<AntiGravityComponent, IsWeightlessEvent>(OnWeightless);

        SubscribeLocalEvent<AntiGravityComponent, StatusEffectAppliedEvent>(OnApplyStatusEffect);
        SubscribeLocalEvent<AntiGravityComponent, StatusEffectRemovedEvent>(OnRemoveStatusEffect);
        SubscribeLocalEvent<AntiGravityComponent, StatusEffectRelayedEvent<IsWeightlessEvent>>(OnWeightlessStatus);

        // Makes sure the IsWeightless event is relayed.
        SubscribeLocalEvent<StatusEffectContainerComponent, IsWeightlessEvent>(_statusEffect.RelayEvent);
    }

    public void OnApplyStatusEffect(Entity<AntiGravityComponent> entity, ref StatusEffectAppliedEvent args)
    {
        if (!HasComp<GravityAffectedComponent>(args.Target))
            return;
        _gravity.RefreshWeightless(args.Target, true);
    }

    public void OnComponentStartup(Entity<AntiGravityComponent> entity, ref ComponentStartup args)
    {
        if (!HasComp<GravityAffectedComponent>(entity.Owner))
            return;
        _gravity.RefreshWeightless(entity.Owner, true);
    }

    public void OnRemoveStatusEffect(Entity<AntiGravityComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if (!HasComp<GravityAffectedComponent>(args.Target))
            return;
        _gravity.RefreshWeightless(args.Target, false);
    }

    public void OnComponentRemoved(Entity<AntiGravityComponent> entity, ref ComponentRemove args)
    {
        if (!HasComp<GravityAffectedComponent>(entity.Owner))
            return;
        _gravity.RefreshWeightless(entity.Owner, false);
    }

    private void OnWeightless(Entity<AntiGravityComponent> entity, ref IsWeightlessEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.IsWeightless = true; // Always be weightless!
    }

    private void OnWeightlessStatus(Entity<AntiGravityComponent> entity, ref StatusEffectRelayedEvent<IsWeightlessEvent> args)
    {
        var weightless = args.Args;
        if (weightless.Handled)
            return;

        weightless.Handled = true;
        weightless.IsWeightless = true;
        args.Args = weightless;
    }
}