using System.Linq;
using Content.Server.Polymorph.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Anomaly.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="PolymorphAnomalyComponent"/>, forcibly polymorphing everything
/// alive in range into a random entity for a random duration whenever the anomaly pulses.
/// </summary>
public sealed class PolymorphAnomalySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Fallback thresholds applied to any polymorph destination that turns out to have no
    /// MobState/MobThresholds configured at all (a content gap on that specific prototype).
    /// Without this, such a creature is structurally incapable of ever reaching Critical/Dead,
    /// no matter how much damage it takes, and revert-on-crit/death would never fire for it.
    /// These are rough generic values, not tuned per-species - tune the actual prototype's own
    /// MobThresholds later and this fallback will simply stop triggering for it.
    /// </summary>
    private const float FallbackCriticalThreshold = 45f;
    private const float FallbackDeadThreshold = 65f;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<Entity<MobStateComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<PolymorphAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnPulse(Entity<PolymorphAnomalyComponent> ent, ref AnomalyPulseEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.PolymorphTable, out var table))
            return;

        var coordinates = _transform.GetMapCoordinates(ent);
        PolymorphInRange(ent, table, coordinates, ent.Comp.Range, args.Severity, permanentChance: 0f);
    }

    private void OnSupercritical(Entity<PolymorphAnomalyComponent> ent, ref AnomalySupercriticalEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.PolymorphTable, out var table))
            return;

        var coordinates = _transform.GetMapCoordinates(ent);

        PolymorphInRange(ent, table, coordinates, ent.Comp.SupercriticalRange, severity: 1f, ent.Comp.SupercriticalPermanentChance);
    }

    /// <summary>
    /// Grabs everything alive in range and forcibly polymorphs each of them independently,
    /// rolling a separate outcome and duration per victim.
    /// </summary>
    private void PolymorphInRange(
        Entity<PolymorphAnomalyComponent> ent,
        PolymorphAnomalyTablePrototype table,
        MapCoordinates coordinates,
        float range,
        float severity,
        float permanentChance)
    {
        if (table.Options.Count == 0)
            return;

        _targets.Clear();
        _lookup.GetEntitiesInRange(coordinates, range, _targets);

        foreach (var target in _targets)
        {
            if (!_mobState.IsAlive(target))
                continue;

            var option = PickOption(table);
            if (!_proto.TryIndex(option.Polymorph, out var polymorphProto))
                continue;

            var permanent = permanentChance > 0f && _random.Prob(permanentChance);

            // TransferEntityInventories requires BOTH the source (the thing being polymorphed)
            // AND the destination (the new form it's spawning into) to have InventoryComponent
            var sourceHasInventory = HasComp<InventoryComponent>(target);
            var destHasInventory = _proto.Index<EntityPrototype>(polymorphProto.Configuration.Entity)
                .TryGetComponent<InventoryComponent>(out _, EntityManager.ComponentFactory);
            var hasInventory = sourceHasInventory && destHasInventory;

            var config = polymorphProto.Configuration with
            {
                Forced = true,
                Inventory = hasInventory ? PolymorphInventoryChange.Transfer : PolymorphInventoryChange.None,
                RevertOnCrit = true,
                RevertOnDeath = true,
                AllowRepeatedMorphs = true,
                Duration = permanent ? null : (int)RollDuration(option, severity).TotalSeconds,
            };

            var child = _polymorph.PolymorphEntity(target, config);
            if (child != null)
                EnsureCanDieOrCrit(child.Value);
        }

        if (ent.Comp.PolymorphSound != null)
            _audio.PlayPvs(ent.Comp.PolymorphSound, ent);
    }

    /// <summary>
    /// Safety net for polymorph destinations that are missing MobState/MobThresholds entirely -
    /// without this, RevertOnCrit/RevertOnDeath can never fire for them since there's no data
    /// telling the game at what damage value to change state. Only fills in fallback values if
    /// the entity has none configured at all; a prototype with its own real MobThresholds is
    /// left completely untouched.
    /// </summary>
    private void EnsureCanDieOrCrit(EntityUid child)
    {
        if (_mobThreshold.TryGetDeadThreshold(child, out _))
            return;

        EnsureComp<MobStateComponent>(child);
        EnsureComp<MobThresholdsComponent>(child);

        _mobThreshold.SetMobStateThreshold(child, FixedPoint2.Zero, MobState.Alive);
        _mobThreshold.SetMobStateThreshold(child, FallbackCriticalThreshold, MobState.Critical);
        _mobThreshold.SetMobStateThreshold(child, FallbackDeadThreshold, MobState.Dead);
    }

    /// <summary>
    /// Picks a random option from the table, weighted by <see cref="PolymorphAnomalyOption.Weight"/>.
    /// </summary>
    private PolymorphAnomalyOption PickOption(PolymorphAnomalyTablePrototype table)
    {
        var totalWeight = table.Options.Sum(o => o.Weight);
        var roll = _random.NextFloat() * totalWeight;

        var cumulative = 0f;
        foreach (var option in table.Options)
        {
            cumulative += option.Weight;
            if (roll <= cumulative)
                return option;
        }

        // Floating point rounding fallback.
        return table.Options[^1];
    }

    /// <summary>
    /// Rolls a duration between an option's min and max, biased towards the max end
    /// as anomaly severity increases.
    /// </summary>
    private TimeSpan RollDuration(PolymorphAnomalyOption option, float severity)
    {
        var t = _random.NextFloat();

        var exponent = MathF.Max(0.1f, 1f - Math.Clamp(severity, 0f, 1f));
        var biasedT = MathF.Pow(t, exponent);

        var seconds = MathHelper.Lerp((float)option.MinDuration.TotalSeconds, (float)option.MaxDuration.TotalSeconds, biasedT);
        return TimeSpan.FromSeconds(seconds);
    }
}
