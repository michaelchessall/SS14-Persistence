using Content.Server.Destructible;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Repairable;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;
using Content.Shared._Persistence14.Atmos.Components;
using JetBrains.Annotations;
using Robust.Shared.Physics.Events;
using Content.Shared.Climbing.Components;

namespace Content.Server._Persistence14.Atmos;

[UsedImplicitly]
public sealed partial class StructureLeakSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly OccluderSystem _occluderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;

    /// <summary>
    ///     So bullets can go through leaking structures.
    /// </summary>
    [SubscribeLocalEvent]
    public void PreventCollide(Entity<StructureLeakComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.Leaking && HasComp<ProjectileComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    public void OnRepaired(Entity<AirtightComponent> airtight, ref RepairedEvent args)
    {
        if (!TryComp<StructureLeakComponent>(airtight.Owner, out var structureLeak))
            return;

        structureLeak.Leaking = false;
        Dirty(airtight.Owner, structureLeak);
        _airtightSystem.SetAirblocked(airtight, true);
        _occluderSystem.SetEnabled(airtight.Owner, true);
        TryComp<ClimbableComponent>(airtight.Owner, out var climbable);
        if (climbable != null)
            RemCompDeferred<ClimbableComponent>(airtight.Owner);

    }
    // TODO: Remove this when upstream switches to DamageDealtEvent
    [SubscribeLocalEvent]
    public void OnExplosions(Entity<DamageableComponent> damage, ref DamageChangedEvent args)
    {
        TryLeak(damage);
    }

    [SubscribeLocalEvent]
    public void OnDamaged(Entity<DamageableComponent> damage, ref DamageDealtEvent args)
    {
        TryLeak(damage);
    }

    public void TryLeak(Entity<DamageableComponent> damage)
    {
        if (!TryComp<StructureLeakComponent>(damage.Owner, out var structureLeak) || !_destructible.TryGetDestroyedAt(damage.Owner, out var destroyedAt) || !TryComp<AirtightComponent>(damage.Owner, out var airtight))
            return;

        if (_damageable.GetTotalDamage(damage.Owner) > destroyedAt * structureLeak.LeakThreshold)
        {
            if (airtight.AirBlocked)
            {
                _airtightSystem.SetAirblocked((damage.Owner, airtight), false);
                _audio.PlayPvs(structureLeak.BreakSound, damage.Owner);
                _occluderSystem.SetEnabled(damage.Owner, false);
                structureLeak.Leaking = true;
                EnsureComp<ClimbableComponent>(damage.Owner);
                Dirty(damage.Owner, structureLeak);
            }
            return;
        }
    }

}
