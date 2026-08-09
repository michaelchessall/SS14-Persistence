using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared._Persistence14.Localization;

namespace Content.Shared._Persistence14.EntityConditions;

/// <summary>
/// Entity Condition based on the current damage values of a damageable component.
/// Capable of handling both individual damage types and total damage.
/// No current support for damage groups.
/// </summary>
public sealed partial class DamageThreshold : EntityConditionBase<DamageThreshold>
{
    [DataField]
    public List<DamageRange> Ranges = new();

    [DataField("min")]
    public FixedPoint2 TotalMinimumDamage = FixedPoint2.Zero;

    [DataField("max")]
    public FixedPoint2 TotalMaximumDamage = FixedPoint2.MaxValue;


    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var conditionStrings = new List<string>();
        var suffix = "";
        foreach (var range in Ranges)
        {
            var typeNames = new List<string>();
            foreach (var type in range.DamageTypes)
            {
                var proto = prototype.Index(type);
                typeNames.Add(proto.LocalizedName);
            }
            typeNames.Sort(StringComparer.CurrentCultureIgnoreCase);
            var types = LocUtils.ConstructLocListAnd(typeNames.ToArray());

            suffix = "";
            if (range.MinimumDamageThreshold > FixedPoint2.Zero)
            {
                if (range.MaximumDamageThreshold < FixedPoint2.MaxValue)
                {
                    suffix = ".between";
                }
                else
                {
                    suffix = ".greater";
                }
            }
            else if (range.MaximumDamageThreshold < FixedPoint2.MaxValue)
            {
                suffix = ".lesser";
            }
            if (suffix == "") continue;

            conditionStrings.Add(Loc.GetString("damage-condition-specific" + suffix,
                ("types", types),
                ("min", range.MinimumDamageThreshold),
                ("max", range.MaximumDamageThreshold)));
        }

        suffix = "";
        if (TotalMinimumDamage > FixedPoint2.Zero)
        {
            if (TotalMaximumDamage < FixedPoint2.MaxValue)
            {
                suffix = ".between";
            }
            else
            {
                suffix = ".greater";
            }
        }
        else if (TotalMaximumDamage < FixedPoint2.MaxValue)
        {
            suffix = ".lesser";
        }
        if (suffix != "")
        {
            conditionStrings.Add(Loc.GetString("damage-condition-total" + suffix,
                ("min", TotalMinimumDamage),
                ("max", TotalMaximumDamage)));
        }

        return LocUtils.ConstructLocListAnd(conditionStrings.ToArray());
    }
}

public sealed partial class DamageThresholdEntityConditionSystem : EntityConditionSystem<DamageableComponent, DamageThreshold>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<DamageThreshold> args)
    {
        // Look, this is a necessary component of this condition. I do not care that offmed is *eventually* coming. I need this *now*.
#pragma warning disable CS0618 // Type or member is obsolete
        var damage = _damageable.GetAllDamage((entity, entity.Comp));
#pragma warning restore CS0618 // Type or member is obsolete

        var totalDamage = damage.GetTotal();

        if (totalDamage < args.Condition.TotalMinimumDamage || totalDamage > args.Condition.TotalMaximumDamage)
        {
            args.Result = false;
            return;
        }

        if (args.Condition.Ranges.Count <= 0)
        {
            args.Result = true;
            return;
        }

        foreach (var state in args.Condition.Ranges)
        {
            var value = FixedPoint2.Zero;
            foreach (var damageType in state.DamageTypes)
            {
                if (damage.DamageDict.TryGetValue(damageType, out var damageValue))
                    value += damageValue;
            }

            if (value < state.MinimumDamageThreshold || value > state.MaximumDamageThreshold)
            {
                args.Result = false;
                return;
            }
        }

        args.Result = true;
    }
}

[DataDefinition]
public sealed partial class DamageRange
{
    [DataField(required: true)]
    public HashSet<ProtoId<DamageTypePrototype>> DamageTypes = new();

    [DataField("min")]
    public FixedPoint2 MinimumDamageThreshold = 0;

    [DataField("max")]
    public FixedPoint2 MaximumDamageThreshold = FixedPoint2.MaxValue;
}