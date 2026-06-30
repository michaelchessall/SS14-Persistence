using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Botany;
using Content.Shared.Burial.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Botany.Systems;

public sealed class PlantHolderSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;

    public const float HydroponicsSpeedMultiplier = 1f;
    public const float HydroponicsConsumptionMultiplier = 1f;
    public readonly FixedPoint2 PlantMetabolismRate = FixedPoint2.New(1);

    private static readonly ProtoId<TagPrototype> HoeTag = "Hoe";
    private static readonly ProtoId<TagPrototype> PlantSampleTakerTag = "PlantSampleTaker";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantHolderComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PlantHolderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlantHolderComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PlantHolderComponent, SolutionTransferredEvent>(OnSolutionTransferred);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantHolderComponent>();
        while (query.MoveNext(out var uid, out var plantHolder))
        {
            if (plantHolder.NextUpdate > _gameTiming.CurTime)
                continue;
            plantHolder.NextUpdate = _gameTiming.CurTime + plantHolder.UpdateDelay;

            Update(uid, plantHolder);
        }
    }

    private int GetCurrentGrowthStage(Entity<PlantHolderComponent> entity)
    {
        var (uid, component) = entity;

        if (component.Seed == null)
            return 0;

        var result = Math.Max(1, (int)(component.Age * component.Seed.GrowthStages / component.Seed.Maturation));
        return result;
    }

    private void OnExamine(Entity<PlantHolderComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (_, component) = entity;

        using (args.PushGroup(nameof(PlantHolderComponent)))
        {
            if (component.Seed == null)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
            }
            else if (!component.Dead)
            {
                var displayName = Loc.GetString(component.Seed.DisplayName);
                args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                    ("seedName", displayName),
                    ("toBeForm", displayName.EndsWith('s') ? "are" : "is")));

                if (component.Health <= component.Seed.Endurance / 2)
                {
                    args.PushMarkup(Loc.GetString(
                        "plant-holder-component-something-already-growing-low-health-message",
                        ("healthState",
                            Loc.GetString(component.Age > component.Seed.Lifespan
                                ? "plant-holder-component-plant-old-adjective"
                                : "plant-holder-component-plant-unhealthy-adjective"))));
                }

                // For future reference, mutations should only appear on examine if they apply to a plant, not to produce.

                if (component.Seed.Ligneous)
                    args.PushMarkup(Loc.GetString("mutation-plant-ligneous"));

                if (component.Seed.TurnIntoKudzu)
                    args.PushMarkup(Loc.GetString("mutation-plant-kudzu"));

                if (component.Seed.CanScream)
                    args.PushMarkup(Loc.GetString("mutation-plant-scream"));

                if (component.Seed.Viable == false)
                    args.PushMarkup(Loc.GetString("mutation-plant-unviable"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-matter-message"));
            }

            if (component.WeedLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-weed-high-level-message"));

            if (component.PestLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-pest-high-level-message"));

            foreach (var nutrientId in component.Nutrients.Keys
                         .OrderByDescending(p => component.Nutrients[p].Required)
                         .ThenByDescending(p => component.Nutrients[p].Bonus)
                         .ThenByDescending(p => component.Nutrients[p].Amount))
            {
                var nutrient = _prototype.Index(nutrientId);
                var nutrientInfo = component.Nutrients[nutrientId];
                var requirementMessage = "";
                var bonusMessage = "";

                if (nutrientInfo.Required > 0 || nutrientInfo.Bonus > 0)
                {
                    requirementMessage = Loc.GetString($"plant-holder-component-nutrient-requirement-message",
                        ("requiredNutrients", nutrientInfo.Required),
                        ("fulfilled", nutrientInfo.Amount >= nutrientInfo.Required));
                }

                if (nutrientInfo.Bonus > 0)
                {
                    bonusMessage = Loc.GetString($"plant-holder-component-nutrient-bonus-message",
                        ("bonusNutrients", nutrientInfo.Bonus + nutrientInfo.Required),
                        ("fulfilled", nutrientInfo.Amount >= nutrientInfo.Bonus + nutrientInfo.Required));
                }

                args.PushMarkup(Loc.GetString($"plant-holder-component-nutrient-level-message",
                    ("name", nutrient.LocalizedName),
                    ("color", nutrient.SubstanceColor),
                    ("nutrientLevel", nutrientInfo.Amount),
                    ("requiredNutrients", nutrientInfo.Required),
                    ("fulfilled", nutrientInfo.Amount >= nutrientInfo.Required),
                    ("requirementMessage", requirementMessage),
                    ("bonusMessage", bonusMessage)));
            }

            if (component.DrawWarnings)
            {
                if (component.Toxins > 40f)
                    args.PushMarkup(Loc.GetString("plant-holder-component-toxins-high-warning"));

                if (component.ImproperLight)
                    args.PushMarkup(Loc.GetString("plant-holder-component-light-improper-warning"));

                if (component.LowHeat)
                    args.PushMarkup(Loc.GetString("plant-holder-component-heat-low-warning"));

                if (component.HighHeat)
                    args.PushMarkup(Loc.GetString("plant-holder-component-heat-high-warning"));

                if (component.LowPressure)
                    args.PushMarkup(Loc.GetString("plant-holder-component-pressure-low-warning"));

                if (component.HighPressure)
                    args.PushMarkup(Loc.GetString("plant-holder-component-pressure-high-warning"));

                if (component.MissingGas > 0)
                    args.PushMarkup(Loc.GetString("plant-holder-component-gas-missing-warning"));
            }
        }
    }

    private void OnInteractUsing(Entity<PlantHolderComponent> entity, ref InteractUsingEvent args)
    {
        var (uid, component) = entity;

        if (TryComp(args.Used, out SeedComponent? seeds))
        {
            if (component.Seed == null)
            {
                if (!_botany.TryGetSeed(seeds, out var seed))
                    return;

                args.Handled = true;
                var name = Loc.GetString(seed.Name);
                var noun = Loc.GetString(seed.Noun);
                _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                    ("seedName", name),
                    ("seedNoun", noun)), args.User, PopupType.Medium);

                component.Seed = seed;
                component.Dead = false;
                component.Age = 1;
                if (seeds.HealthOverride != null)
                {
                    component.Health = seeds.HealthOverride.Value;
                }
                else
                {
                    component.Health = component.Seed.Endurance;
                }
                component.LastCycle = _gameTiming.CurTime;

                if (TryComp<PaperLabelComponent>(args.Used, out var paperLabel))
                {
                    _itemSlots.TryEjectToHands(args.Used, paperLabel.LabelSlot, args.User);
                }
                QueueDel(args.Used);

                CheckLevelSanity(uid, component);
                UpdateSprite(uid, component);
                SeedUpdated(uid, component);

                if (seed.PlantLogImpact != null)
                    _adminLogger.Add(LogType.Botany, seed.PlantLogImpact.Value, $"{ToPrettyString(args.User):player} planted  {Loc.GetString(seed.Name):seed} at Pos:{Transform(uid).Coordinates}.");

                return;
            }

            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("plant-holder-component-already-seeded-message",
                ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
            return;
        }

        if (_tagSystem.HasTag(args.Used, HoeTag))
        {
            args.Handled = true;
            if (component.WeedLevel > 0)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-remove-weeds-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                _popup.PopupEntity(Loc.GetString("plant-holder-component-remove-weeds-others-message",
                    ("otherName", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User), true);
                component.WeedLevel = 0;
                UpdateSprite(uid, component);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-no-weeds-message"), args.User);
            }

            return;
        }

        if (HasComp<ShovelComponent>(args.Used))
        {
            args.Handled = true;
            if (component.Seed != null)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-remove-plant-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
                _popup.PopupEntity(Loc.GetString("plant-holder-component-remove-plant-others-message",
                    ("name", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User), true);
                RemovePlant(uid, component);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-no-plant-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User);
            }

            return;
        }

        if (_tagSystem.HasTag(args.Used, PlantSampleTakerTag))
        {
            args.Handled = true;
            if (component.Seed == null)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-nothing-to-sample-message"), args.User);
                return;
            }

            if (component.Sampled)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-already-sampled-message"), args.User);
                return;
            }

            if (component.Dead)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-dead-plant-message"), args.User);
                return;
            }

            if (GetCurrentGrowthStage(entity) <= 1)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-early-sample-message"), args.User);
                return;
            }

            component.Health -= (_random.Next(3, 5) * 10);

            float? healthOverride;
            if (component.Harvest)
            {
                healthOverride = null;
            }
            else
            {
                healthOverride = component.Health;
            }
            component.Seed.Unique = false;
            var packetSeed = component.Seed;
            var seed = _botany.SpawnSeedPacket(packetSeed, Transform(args.User).Coordinates, args.User, healthOverride);
            _randomHelper.RandomOffset(seed, 0.25f);
            var displayName = Loc.GetString(component.Seed.DisplayName);
            _popup.PopupCursor(Loc.GetString("plant-holder-component-take-sample-message",
                ("seedName", displayName)), args.User);

            DoScream(entity.Owner, component.Seed);

            if (_random.Prob(0.3f))
                component.Sampled = true;

            // Just in case.
            CheckLevelSanity(uid, component);
            ForceUpdateByExternalCause(uid, component);

            return;
        }

        if (HasComp<SharpComponent>(args.Used))
        {
            args.Handled = true;
            DoHarvest(uid, args.User, component);
            return;
        }

        if (TryComp<ProduceComponent>(args.Used, out var produce))
        {
            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("plant-holder-component-compost-message",
                ("owner", uid),
                ("usingItem", args.Used)), args.User, PopupType.Medium);
            _popup.PopupEntity(Loc.GetString("plant-holder-component-compost-others-message",
                ("user", Identity.Entity(args.User, EntityManager)),
                ("usingItem", args.Used),
                ("owner", uid)), uid, Filter.PvsExcept(args.User), true);

            if (_solutionContainerSystem.TryGetSolution(args.Used, produce.SolutionName, out var soln2, out var solution2))
            {
                if (_solutionContainerSystem.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution1))
                {
                    // We try to fit as much of the composted plant's contained solution into the hydroponics tray as we can,
                    // since the plant will be consumed anyway.

                    var fillAmount = FixedPoint2.Min(solution2.Volume, solution1.AvailableVolume);
                    _solutionContainerSystem.TryAddSolution(component.SoilSolution.Value, _solutionContainerSystem.SplitSolution(soln2.Value, fillAmount));

                    ForceUpdateByExternalCause(uid, component);
                }
            }
            var seed = produce.Seed;
            if (seed != null)
            {
                var nutrientBonus = seed.Potency / 2.5f;
                AdjustNutrient(uid, nutrientBonus, "nutrient", component);
            }
            QueueDel(args.Used);
        }
    }

    private void OnSolutionTransferred(Entity<PlantHolderComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPvs(ent.Comp.WateringSound, ent.Owner);
    }
    private void OnInteractHand(Entity<PlantHolderComponent> entity, ref InteractHandEvent args)
    {
        DoHarvest(entity, args.User, entity.Comp);
    }

    public void WeedInvasion()
    {
        // TODO
    }


    public void Update(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        UpdateReagents(uid, component);

        var curTime = _gameTiming.CurTime;

        if (component.ForceUpdate)
            component.ForceUpdate = false;
        else if (curTime < (component.LastCycle + component.CycleDelay))
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);
            return;
        }

        component.LastCycle = curTime;

        // Process mutations
        if (component.MutationLevel > 0)
        {
            Mutate(uid, Math.Min(component.MutationLevel, 25), component);
            component.UpdateSpriteAfterUpdate = true;
            component.MutationLevel = 0;
        }

        // Weeds might need to be changed due to the plant nutrient rework
        if (CheckRequiredNutrients(uid, component))
        {
            var chance = 0f;
            if (component.Seed == null)
                chance = 0.05f;
            else if (component.Seed.TurnIntoKudzu)
                chance = 1f;
            else
                chance = 0.01f;

            if (_random.Prob(chance))
                component.WeedLevel += 1 + HydroponicsSpeedMultiplier * component.WeedCoefficient;

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        if (component.Seed != null && component.Seed.TurnIntoKudzu
            && component.WeedLevel >= component.Seed.WeedHighLevelThreshold)
        {
            Spawn(component.Seed.KudzuPrototype, Transform(uid).Coordinates.SnapToGrid(EntityManager));
            component.Seed.TurnIntoKudzu = false;
            component.Health = 0;
        }

        // There's a chance for a weed explosion to happen if weeds take over.
        // Plants that are themselves weeds (WeedTolerance > 8) are unaffected.
        if (component.WeedLevel >= 10 && _random.Prob(0.1f))
        {
            if (component.Seed == null || component.WeedLevel >= component.Seed.WeedTolerance + 2)
                WeedInvasion();
        }

        // If we have no seed planted, or the plant is dead, stop processing here.
        if (component.Seed == null || component.Dead)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);

            return;
        }

        // There's a small chance the pest population increases.
        // Can only happen when there's a live seed planted.
        if (_random.Prob(0.01f))
        {
            component.PestLevel += 0.5f * HydroponicsSpeedMultiplier;
            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        // Advance plant age here.
        if (component.SkipAging > 0)
            component.SkipAging--;
        else
        {
            if (_random.Prob(0.8f))
                component.Age += (int)(1 * HydroponicsSpeedMultiplier);

            component.UpdateSpriteAfterUpdate = true;
        }

        var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;

        // Make sure genetics are viable.
        if (!component.Seed.Viable)
        {
            AffectGrowth(uid, -1, component);
            component.Health -= 6 * healthMod;
        }

        // Prevents the plant from aging when lacking resources.
        // Limits the effect on aging so that when resources are added, the plant starts growing in a reasonable amount of time.
        if (component.SkipAging < 10)
        {
            // Make sure the plant is not starving.
            if (CheckRequiredNutrients(uid, component))
            {
                component.Health += Convert.ToInt32(_random.Prob(0.7f)) * healthMod;
            }
            else if (component.HarvestAge)
            {
                AffectGrowth(uid, -1, component);
                component.Health -= healthMod;
            }

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        var environment = _atmosphere.GetContainingMixture(uid, true, true) ?? GasMixture.SpaceGas;

        component.MissingGas = 0;
        if (component.Seed.ConsumeGasses.Count > 0)
        {
            foreach (var (gas, amount) in component.Seed.ConsumeGasses)
            {
                if (environment.GetMoles(gas) < amount)
                {
                    component.MissingGas++;
                    continue;
                }

                environment.AdjustMoles(gas, -amount);
            }

            if (component.MissingGas > 0)
            {
                component.Health -= component.MissingGas * HydroponicsSpeedMultiplier;
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }
        }

        // SeedPrototype pressure resistance.
        var pressure = environment.Pressure;

        var lowPressure = pressure < component.MinimumPressure;
        var highPressure = pressure > component.MaximumPressure;

        if (component.LowPressure || component.HighPressure)
        {
            component.Health -= healthMod;
        }

        // SeedPrototype ideal temperature.

        var lowHeat = environment.Temperature < component.MinimumTemperature;
        var highHeat = environment.Temperature > component.MaximumTemperature;

        if (component.LowHeat || component.HighHeat)
        {
            component.Health -= healthMod;
        }

        if (component.DrawWarnings &&
            (component.LowHeat != lowHeat || component.HighHeat != highHeat || component.LowPressure != lowPressure || component.HighPressure != highPressure))
        {
            component.UpdateSpriteAfterUpdate = true;
        }
        component.LowHeat = lowHeat;
        component.HighHeat = highHeat;
        component.LowPressure = lowPressure;
        component.HighPressure = highPressure;

        // Gas production.
        var exudeCount = component.Seed.ExudeGasses.Count;
        if (exudeCount > 0)
        {
            foreach (var (gas, amount) in component.Seed.ExudeGasses)
            {
                environment.AdjustMoles(gas,
                    MathF.Max(1f, MathF.Round(amount * MathF.Round(component.Seed.Potency) / exudeCount)));
            }
        }

        // Toxin levels beyond the plant's tolerance cause damage.
        // They are, however, slowly reduced over time.
        /*if (component.Toxins > 0)
        {
            var toxinUptake = MathF.Max(1, MathF.Round(component.Toxins / 10f));
            if (component.Toxins > component.Seed.ToxinsTolerance)
            {
                component.Health -= toxinUptake;
            }

            component.Toxins -= toxinUptake;
            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        // Weed levels.
        if (component.PestLevel > 0)
        {
            // TODO: Carnivorous plants?
            if (component.PestLevel > component.Seed.PestTolerance)
            {
                component.Health -= HydroponicsSpeedMultiplier;
            }

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }*/

        // Weed levels.
        if (component.WeedLevel > 0)
        {
            // TODO: Parasitic plants.
            if (component.WeedLevel >= component.Seed.WeedTolerance)
            {
                component.Health -= HydroponicsSpeedMultiplier;
            }

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        if (component.Age > component.Seed.Lifespan)
        {
            component.Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }
        else if (component.Age < 0) // Revert back to seed packet!
        {
            var packetSeed = component.Seed;
            // will put it in the trays hands if it has any, please do not try doing this
            _botany.SpawnSeedPacket(packetSeed, Transform(uid).Coordinates, uid);
            RemovePlant(uid, component);
            component.ForceUpdate = true;
            Update(uid, component);
            return;
        }

        CheckHealth(uid, component);

        //if (component.Harvest && component.Seed.HarvestRepeat == HarvestType.SelfHarvest)
        //    AutoHarvest(uid, component);

        component.HarvestAge = component.Age - component.LastProduce > component.Seed.Production;

        // If enough time has passed since the plant was harvested, we're ready to harvest again!
        if (!component.Dead && component.Seed.ProductPrototypes.Count > 0)
        {
            if (component.Age > component.Seed.Production && CheckRequiredNutrients(uid, component))
            {
                if (component.HarvestAge && !component.Harvest)
                {
                    component.Harvest = true;
                    component.LastProduce = component.Age;
                }
            }
            else
            {
                if (component.Harvest)
                {
                    component.Harvest = false;
                    component.LastProduce = component.Age;
                }
            }
        }

        CheckLevelSanity(uid, component);

        if (component.UpdateSpriteAfterUpdate)
            UpdateSprite(uid, component);
    }

    //TODO: kill this bullshit
    public void CheckLevelSanity(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed != null)
            component.Health = MathHelper.Clamp(component.Health, 0, component.Seed.Endurance);
        else
        {
            component.Health = 0f;
            component.Dead = false;
        }

        component.MutationLevel = MathHelper.Clamp(component.MutationLevel, 0f, 100f);
        component.NutritionLevel = MathHelper.Clamp(component.NutritionLevel, 0f, 100f);
        component.WaterLevel = MathHelper.Clamp(component.WaterLevel, 0f, 100f);
        component.PestLevel = MathHelper.Clamp(component.PestLevel, 0f, 10f);
        component.WeedLevel = MathHelper.Clamp(component.WeedLevel, 0f, 10f);
        component.Toxins = MathHelper.Clamp(component.Toxins, 0f, 100f);
        component.YieldMod = MathHelper.Clamp(component.YieldMod, 0, 2);
        component.MutationMod = MathHelper.Clamp(component.MutationMod, 0f, 3f);
    }

    public bool DoHarvest(EntityUid plantholder, EntityUid user, PlantHolderComponent? component = null)
    {
        if (!Resolve(plantholder, ref component))
            return false;

        if (component.Seed == null || Deleted(user))
            return false;


        if (component.Harvest && !component.Dead)
        {
            if (_hands.TryGetActiveItem(user, out var activeItem))
            {
                if (!_botany.CanHarvest(component.Seed, activeItem))
                {
                    _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), user);
                    return false;
                }
            }
            else if (!_botany.CanHarvest(component.Seed))
            {
                return false;
            }

            if (!CheckRequiredNutrients(plantholder, component)) // This should only happen if a reagent that removes nutrients is added to the plant.
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-low-nutrient-cant-harvest-message"), user);
                component.Harvest = false;
                UpdateSprite(plantholder, component);
                return false;
            }

            _botany.Harvest(component.Seed, user, component.Nutrients, component.YieldMod);
            AfterHarvest(plantholder, component);
            return true;
        }

        if (!component.Dead)
            return false;

        RemovePlant(plantholder, component);
        AfterHarvest(plantholder, component);
        return true;
    }

    /// <summary>
    /// Force do scream on PlantHolder (like plant is screaming) using seed's ScreamSound specifier (collection or soundPath)
    /// </summary>
    /// <returns></returns>
    public bool DoScream(EntityUid plantholder, SeedData? seed = null)
    {
        if (seed == null || seed.CanScream == false)
            return false;

        _audio.PlayPvs(seed.ScreamSound, plantholder);
        return true;
    }

    public void AutoHarvest(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed == null || !component.Harvest)
            return;

        if (!CheckRequiredNutrients(uid, component)) // This should only happen if a reagent that removes nutrients is added to the plant.
        {
            return;
        }

        _botany.AutoHarvest(component.Seed, Transform(uid).Coordinates, component.Nutrients, component.YieldMod);
        AfterHarvest(uid, component);
    }

    private void AfterHarvest(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Harvest = false;
        component.LastProduce = component.Age;

        DoScream(uid, component.Seed);

        if (!component.Dead)
        {
            foreach (var nutrient in component.Nutrients)
            {
                AdjustNutrient(uid, (nutrient.Value.Required + nutrient.Value.Bonus) * -1, nutrient.Key, component);
            }
        }

        if (component.Seed?.HarvestRepeat == HarvestType.NoRepeat)
            RemovePlant(uid, component);

        CheckLevelSanity(uid, component);
        UpdateSprite(uid, component);
    }

    public void CheckHealth(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Health <= 0)
        {
            Die(uid, component);
        }
    }

    public void Die(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Dead = true;
        component.Harvest = false;
        component.MutationLevel = 0;
        component.YieldMod = 1;
        component.MutationMod = 1;
        component.ImproperLight = false;
        component.LowHeat = false;
        component.HighHeat = false;
        component.LowPressure = false;
        component.HighPressure = false;
        component.WeedLevel += 1 * HydroponicsSpeedMultiplier;
        component.PestLevel = 0;
        UpdateSprite(uid, component);
    }

    public void RemovePlant(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.YieldMod = 1;
        component.MutationMod = 1;
        component.PestLevel = 0;
        component.Seed = null;
        component.Dead = false;
        component.Age = 0;
        component.LastProduce = 0;
        component.Sampled = false;
        component.Harvest = false;
        component.ImproperLight = false;
        component.LowHeat = false;
        component.HighHeat = false;
        component.LowPressure = false;
        component.HighPressure = false;

        UpdateSprite(uid, component);
        SeedUpdated(uid, component);
    }

    public void AffectGrowth(EntityUid uid, int amount, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed == null)
            return;

        if (amount > 0)
        {
            if (component.Age < component.Seed.Maturation)
                component.Age += amount;
            else if (!component.Harvest && component.Seed.Yield <= 0f)
                component.LastProduce -= amount;
        }
        else
        {
            if (component.Age < component.Seed.Maturation)
                component.SkipAging++;
            else if (!component.Harvest && component.Seed.Yield <= 0f)
                component.LastProduce += amount;
        }
    }

    public void AdjustNutrient(EntityUid uid, FixedPoint2 amount, ProtoId<PlantNutrientPrototype> nutrientId, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var nutrient = component.Nutrients.GetOrNew(nutrientId);

        nutrient.Amount += amount;

        if (nutrient.Amount > 0)
            return;

        nutrient.Amount = 0;
        RemoveEmptyNutrient(uid, nutrientId, component);
    }

    private void RemoveEmptyNutrient(EntityUid uid, ProtoId<PlantNutrientPrototype> nutrient, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        var nutrientInfo = component.Nutrients[nutrient];
        if (nutrientInfo.Amount == 0 && nutrientInfo.Required == 0 && nutrientInfo.Bonus == 0)
            component.Nutrients.Remove(nutrient);
    }

    public void UpdateNutrientRequirements(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        foreach (var nutrientInfo in component.Nutrients.Values)
        {
            nutrientInfo.Required = 0;
            nutrientInfo.Bonus = 0;
        }

        if (component.Seed != null && !component.Dead)
        {
            var yield = component.Seed.Yield;
            foreach (var requirement in component.Seed.Requirements)
            {
                var nutrient = component.Nutrients.GetOrNew(requirement.Key);

                nutrient.Required += requirement.Value.Requirement * yield;
                nutrient.Bonus += requirement.Value.BonusRequirement * yield;
            }

            foreach (var requirement in component.Seed.Chemicals.Values.SelectMany(quantity => quantity.Requirements))
            {
                var nutrient = component.Nutrients.GetOrNew(requirement.Key);

                nutrient.Required += requirement.Value.Requirement * yield;
                nutrient.Bonus += requirement.Value.BonusRequirement * yield;
            }
        }

        foreach (var nutrientId in component.Nutrients.Keys)
        {
            RemoveEmptyNutrient(uid, nutrientId, component);
        }
    }

    public bool CheckRequiredNutrients(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (component.Dead)
            return false;


        foreach (var nutrientInfo in component.Nutrients.Values)
        {
            if (nutrientInfo.Amount < nutrientInfo.Required)
                return false;
        }

        return true;
    }

    public void UpdateTolerances(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (component.Seed == null)
        {
            component.MinimumTemperature = float.NegativeInfinity;
            component.MaximumTemperature = float.PositiveInfinity;
            component.MinimumPressure = float.NegativeInfinity;
            component.MaximumPressure = float.PositiveInfinity;
            return;
        }

        var idealHeat = component.Seed.BaseIdealHeat;
        var heatTolerance = component.Seed.BaseHeatTolerance;
        var idealPressure =  component.Seed.BaseIdealPressure;
        var pressureTolerance = component.Seed.BasePressureTolerance;

        foreach (var quantity in component.Seed.Chemicals.Values)
        {
            idealHeat += quantity.ModifyIdealHeat;
            heatTolerance += quantity.ModifyHeatTolerance;
            idealPressure += quantity.ModifyIdealPressure;
            pressureTolerance += quantity.ModifyPressureTolerance;
        }

        component.MinimumTemperature = idealHeat - heatTolerance;
        component.MaximumTemperature = idealHeat + heatTolerance;
        component.MinimumPressure = idealPressure - pressureTolerance;
        component.MaximumPressure = idealPressure + pressureTolerance;

    }

    public void SeedUpdated(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        UpdateNutrientRequirements(uid, component);
        UpdateTolerances(uid, component);
    }

    public void UpdateReagents(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_solutionContainerSystem.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution))
            return;

        if (solution.Volume > 0 && component.MutationLevel < 25)
        {
            // Don't apply any effects to a non-unique seed ever! Remove this when botany code is sane...
            EnsureUniqueSeed(uid, component);
            foreach (var entry in solution.Contents)
            {
                if (entry.Quantity < PlantMetabolismRate)
                    continue;

                var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
                _entityEffects.ApplyEffects(uid, reagentProto.PlantMetabolisms.ToArray(), entry.Quantity);
            }
            SeedUpdated(uid, component);
            _solutionContainerSystem.RemoveEachReagent(component.SoilSolution.Value, PlantMetabolismRate);
        }

        CheckLevelSanity(uid, component);
    }

    private void Mutate(EntityUid uid, float severity, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed != null)
        {
            EnsureUniqueSeed(uid, component);
            _mutation.MutateSeed(uid, ref component.Seed, severity);
            SeedUpdated(uid, component);
        }
    }

    public void UpdateSprite(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.UpdateSpriteAfterUpdate = false;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (component.Seed != null)
        {
            if (component.DrawWarnings)
            {
                _appearance.SetData(uid, PlantHolderVisuals.HealthLight, component.Health <= component.Seed.Endurance / 2f);
            }

            if (component.Dead)
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, "dead", app);
            }
            else if (component.Harvest)
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, "harvest", app);
            }
            else if (component.Age < component.Seed.Maturation)
            {
                var growthStage = GetCurrentGrowthStage((uid, component));

                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{growthStage}", app);
                component.LastProduce = component.Age;
            }
            else
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{component.Seed.GrowthStages}", app);
            }
        }
        else
        {
            _appearance.SetData(uid, PlantHolderVisuals.PlantState, "", app);
            _appearance.SetData(uid, PlantHolderVisuals.HealthLight, false, app);
        }

        if (!component.DrawWarnings)
            return;

        _appearance.SetData(uid, PlantHolderVisuals.WaterLight, component.WaterLevel <= 15, app);
        _appearance.SetData(uid, PlantHolderVisuals.NutritionLight, !CheckRequiredNutrients(uid, component) && component.HarvestAge, app);
        _appearance.SetData(uid, PlantHolderVisuals.AlertLight,
            component.WeedLevel >= 5 || component.PestLevel >= 5 || component.Toxins >= 40 || component.LowHeat || component.HighHeat ||
            component.ImproperLight || component.LowPressure || component.HighPressure || component.MissingGas > 0, app);
        _appearance.SetData(uid, PlantHolderVisuals.HarvestLight, component.Harvest, app);
    }

    /// <summary>
    ///     Check if the currently contained seed is unique. If it is not, clone it so that we have a unique seed.
    ///     Necessary to avoid modifying global seeds.
    /// </summary>
    public void EnsureUniqueSeed(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Seed is { Unique: false })
            component.Seed = component.Seed.Clone();
    }

    public void ForceUpdateByExternalCause(EntityUid uid, PlantHolderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.SkipAging++; // We're forcing an update cycle, so one age hasn't passed.
        component.ForceUpdate = true;
        Update(uid, component);
    }
}
