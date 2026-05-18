using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Robust.Shared.Utility;

namespace Content.Server.Power.Generation.GasGenerator;

/// <summary>
/// Handles processing logic for the gas generator.
/// </summary>
/// <remarks>
/// <para>
/// The gas generator consumes fuel gas mixtures through an inlet and expels waste through an outlet.
/// Power generation depends on two factors:
/// 1. Fuel composition: Optimal methane/oxygen ratio produces maximum efficiency
/// 2. Temperature: Hotter fuel produces more power
/// </para>
/// <para>
/// The generator's power output is adjusted based on the combined efficiency of these factors,
/// and it will consume more or less fuel depending on current efficiency.
/// </para>
/// </remarks>
public sealed partial class GasGeneratorSystem : EntitySystem
{
    [Dependency] private AmbientSoundSystem _ambientSound = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasGeneratorComponent, AtmosDeviceUpdateEvent>(GeneratorUpdate);
        SubscribeLocalEvent<GasGeneratorComponent, PowerChangedEvent>(GeneratorPowerChange);
        SubscribeLocalEvent<GasGeneratorComponent, ExaminedEvent>(GeneratorExamined);
        SubscribeLocalEvent<GasGeneratorComponent, ComponentInit>(OnComponentInit);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void OnComponentInit(EntityUid uid, GasGeneratorComponent component, ComponentInit args)
    {
        // Initialize internal atmosphere with specified volume
        component.InternalAtmosphere = new GasMixture(component.InternalVolume);

        // If the entity has a node container with an inlet pipe node, apply configured inlet volume
        if (_nodeContainerQuery.TryGetComponent(uid, out var nodeContainer))
        {
            if (nodeContainer.Nodes.TryGetValue(GasGeneratorComponent.NodeNameInlet, out var inletNode) && inletNode is PipeNode pipe)
            {
                pipe.Volume = component.InletNodeVolume;
            }
        }
    }

    private void GeneratorExamined(EntityUid uid, GasGeneratorComponent component, ExaminedEvent args)
    {
        var supplier = Comp<PowerSupplierComponent>(uid);

        using (args.PushGroup(nameof(GasGeneratorComponent)))
        {
            // Display CurrentSupply (what network is drawing) like TEG does
            args.PushMarkup(Loc.GetString("gas-generator-examine-power", ("power", (int)supplier.CurrentSupply)));
            args.PushMarkup(Loc.GetString("gas-generator-examine-efficiency", ("efficiency", $"{component.CurrentEfficiency * 100:F0}")));
            args.PushMarkup(Loc.GetString("gas-generator-examine-consumption", ("consumption", $"{component.CurrentConsumptionRate:F1}")));
        }
    }

    private void GeneratorUpdate(EntityUid uid, GasGeneratorComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var supplier = Comp<PowerSupplierComponent>(uid);
        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);

        if (!powerReceiver.Powered || !component.Enabled)
        {
            supplier.MaxSupply = 0;
            component.CurrentConsumptionRate = 0;
            _ambientSound.SetAmbience(uid, false);
            _appearance.SetData(uid, PowerDeviceVisuals.Powered, false);
            return;
        }

        // Ensure internal atmosphere exists
        component.InternalAtmosphere ??= new GasMixture(component.InternalVolume);

        // Get the inlet pipe
        if (!TryGetPipes(uid, out var inlet))
        {
            supplier.MaxSupply = 0;
            _ambientSound.SetAmbience(uid, false);
            _appearance.SetData(uid, PowerDeviceVisuals.Powered, false);
            return;
        }

        DebugTools.Assert(inlet != null);
        var inletMixture = inlet!.Air;
        var internalMixture = component.InternalAtmosphere;

        // STEP 1: Internal pump - transfer gas from inlet to internal chamber with pressure limiting
        var currentPressure = internalMixture.Pressure;

        if (currentPressure < component.MaxInternalPressure)
        {
            // Calculate how much we can transfer based on pressure differential and flow rate
            var pressureRatio = currentPressure / component.MaxInternalPressure;
            var flowMultiplier = 1.0f - pressureRatio; // Reduces as pressure approaches max

            // Maximum moles we can transfer this tick based on flow rate
            var maxTransfer = component.MaxInletFlowRate * args.dt * flowMultiplier;

            // Transfer available gas up to flow limit
            var availableTransfer = Math.Min(inletMixture.TotalMoles, maxTransfer);

            if (availableTransfer > 0.01f)
            {
                // Transfer gas from inlet to internal chamber
                var transferredGas = inletMixture.Remove(availableTransfer);
                _atmosphere.Merge(internalMixture, transferredGas);
            }
        }

        // STEP 2: Check if we have enough fuel in internal chamber for combustion
        // Generator should only work when internal chamber is reasonably full (not just trace amounts)
        var availablePrimary = internalMixture.GetMoles(component.InputGas1);
        var availableSecondary = internalMixture.GetMoles(component.InputGas2);

        // Don't activate if either input gas is missing
        if (availablePrimary <= 0 || availableSecondary <= 0)
        {
            supplier.MaxSupply = 0;
            component.CurrentConsumptionRate = 0;
            component.CurrentEfficiency = 0;
            _ambientSound.SetAmbience(uid, false);
            _appearance.SetData(uid, PowerDeviceVisuals.Powered, false);
            return;
        }

        // Ensure we have minimum amounts of BOTH gases based on their optimal ratio
        // Use 10% of capacity instead of 50% so generator can continue operating through combustion cycles
        var minRequiredMoles = (component.MaxInternalPressure * component.InternalVolume) / (Atmospherics.R * internalMixture.Temperature) * 0.1f; // At least 10% capacity
        var minPrimary = minRequiredMoles * component.OptimalInputRatio;
        var minSecondary = minRequiredMoles * (1.0f - component.OptimalInputRatio);

        if (availablePrimary < minPrimary || availableSecondary < minSecondary)
        {
            supplier.MaxSupply = 0;
            component.CurrentConsumptionRate = 0;
            component.CurrentEfficiency = 0;
            _ambientSound.SetAmbience(uid, false);
            _appearance.SetData(uid, PowerDeviceVisuals.Powered, false);
            return;
        }

        // STEP 3: Consume from internal chamber for combustion

        // Additional safety check (redundant but explicit)
        if (availablePrimary <= 0 || availableSecondary <= 0)
        {
            component.CurrentConsumptionRate = 0;
            component.CurrentEfficiency = 0;
            supplier.MaxSupply = 0;
            _ambientSound.SetAmbience(uid, false);
            _appearance.SetData(uid, PowerDeviceVisuals.Powered, false);
            return;
        }

        // Collect incompatible gases first
        var incompatibleGases = new Dictionary<Gas, float>();
        foreach (var gas in new[] { Gas.Oxygen, Gas.Nitrogen, Gas.CarbonDioxide, Gas.Plasma, Gas.Tritium, Gas.WaterVapor })
        {
            if (gas != component.InputGas1 && gas != component.InputGas2)
            {
                var moles = internalMixture.GetMoles(gas);
                if (moles > 0)
                {
                    incompatibleGases[gas] = moles;
                    internalMixture.AdjustMoles(gas, -moles);
                }
            }
        }

        // Calculate mixture ratio BEFORE consumption to determine consumption rate
        var primaryFraction = availablePrimary / (availablePrimary + availableSecondary);
        var actualRatio = availablePrimary / Math.Max(availableSecondary, 0.01f);
        var normalizedRatio = actualRatio / component.OptimalInputRatio;

        // Consumption rate scales based on lean/rich ratio
        // Rich mixtures (normalizedRatio > 1.0) consume faster
        // Lean mixtures (normalizedRatio < 1.0) consume slower
        var consumptionMultiplier = 1.0f;
        if (normalizedRatio > 1.0f)
            consumptionMultiplier = 1.0f + Math.Min(normalizedRatio - 1.0f, 1.0f) * 0.5f; // Up to 1.5x faster when rich
        else if (normalizedRatio < 1.0f)
            consumptionMultiplier = 1.0f - (1.0f - normalizedRatio) * 0.25f; // Down to 0.75x slower when lean

        // Consume gas based on mixture ratio
        var consumed = availablePrimary * component.BaseConsumptionFraction * consumptionMultiplier; // Base rate from YAML
        var secondaryConsumed = availableSecondary * component.BaseConsumptionFraction * consumptionMultiplier;

        // Clamp to available amounts
        consumed = Math.Min(consumed, availablePrimary);
        secondaryConsumed = Math.Min(secondaryConsumed, availableSecondary);

        internalMixture.AdjustMoles(component.InputGas1, -consumed);
        internalMixture.AdjustMoles(component.InputGas2, -secondaryConsumed);

        // Calculate efficiency based on composition only
        var (compositionEfficiency, powerMultiplier, fuelMultiplier) = CalculateCompositionEfficiency(internalMixture, component);
        var temperatureEfficiency = 1.0f;

        // Apply composition tradeoffs to power and consumption
        component.CurrentEfficiency = MathHelper.Clamp(
            component.FuelEfficiency * compositionEfficiency * temperatureEfficiency,
            0f, 1f);

        // Power scales directly with consumption: power per mole of fuel consumed
        var powerPerMole = component.MaxPowerOutput / component.MaxFuelConsumptionRate;
        // Apply configurable scaling and boosts (previously hardcoded multipliers)
        var power = consumed * powerPerMole * component.PowerScaleMultiplier * component.CurrentEfficiency * powerMultiplier * component.PowerExtraBoost;

        // Clamp power to max output
        power = Math.Min(power, component.MaxPowerOutput);

        supplier.MaxSupply = power;

        component.CurrentConsumptionRate = (consumed / args.dt) * fuelMultiplier;

        // Recalculate mixture ratio after consumption to determine exhaust temperature
        var newPrimaryMoles = internalMixture.GetMoles(component.InputGas1);
        var newSecondaryMoles = internalMixture.GetMoles(component.InputGas2);
        var richness = (newPrimaryMoles / (newPrimaryMoles + newSecondaryMoles)) - component.OptimalInputRatio;
        var exhaustTempAdjust = -richness * 200f; // ±200K based on richness

        var exhaustMixture = new GasMixture { Temperature = Atmospherics.T20C };

        // Only generate exhaust if fuel was actually consumed
        if (consumed > 0.01f)
        {
            // Calculate combustion heat released using configured energy per mole
            var combustionEnergy = component.CombustionEnergyPerMole * consumed;

            // Generate exhaust based on mixture leanness/richness and actual consumption
            // Rich mixture (excess fuel, normalizedRatio > 1.0): Incomplete combustion
            if (normalizedRatio > 1.0f)
            {
                var richnessFactor = Math.Min(normalizedRatio - 1.0f, 1.0f); // 0 to 1

                // Less complete combustion products
                var completionFactor = 1.0f - (richnessFactor * 0.4f); // 100% down to 60%
                exhaustMixture.AdjustMoles(component.WasteGas1, consumed * component.WasteGas1Ratio * completionFactor);
                exhaustMixture.AdjustMoles(component.WasteGas2, consumed * component.WasteGas2Ratio * completionFactor);

                // More unburned fuel escapes
                var richSlipRate = component.FuelSlipRate + (richnessFactor * 0.3f); // Up to +30% slip
                exhaustMixture.AdjustMoles(component.InputGas1, consumed * richSlipRate);
            }
            // Lean mixture (excess oxygen, normalizedRatio < 1.0): Complete combustion
            else if (normalizedRatio < 1.0f)
            {
                var leannessFactor = 1.0f - normalizedRatio; // 0 to 1

                // Complete combustion products based on fuel consumed
                exhaustMixture.AdjustMoles(component.WasteGas1, consumed * component.WasteGas1Ratio);
                exhaustMixture.AdjustMoles(component.WasteGas2, consumed * component.WasteGas2Ratio);

                // Excess oxygen passes through (what we took in minus what was needed)
                var oxigenNeeded = consumed * component.InputGas2Ratio;
                var excessOxygen = secondaryConsumed - oxigenNeeded;
                exhaustMixture.AdjustMoles(component.InputGas2, Math.Max(excessOxygen, 0));

                // Minimal fuel slip in lean conditions
                var leanSlipRate = component.FuelSlipRate * (1.0f - leannessFactor * 0.5f); // Up to -50% slip
                exhaustMixture.AdjustMoles(component.InputGas1, consumed * leanSlipRate);
            }
            // Stoichiometric: Ideal combustion
            else
            {
                exhaustMixture.AdjustMoles(component.WasteGas1, consumed * component.WasteGas1Ratio);
                exhaustMixture.AdjustMoles(component.WasteGas2, consumed * component.WasteGas2Ratio);
                exhaustMixture.AdjustMoles(component.InputGas1, consumed * component.FuelSlipRate);
            }

            // Add incompatible gases to exhaust
            foreach (var (incompatibleGas, moles) in incompatibleGases)
            {
                exhaustMixture.AdjustMoles(incompatibleGas, moles);
            }

            // Apply combustion heat to exhaust mixture (heat exhaust like a real burn reaction)
            var exhaustHeatCapacity = _atmosphere.GetHeatCapacity(exhaustMixture, true);
            if (exhaustHeatCapacity > Atmospherics.MinimumHeatCapacity && combustionEnergy > 0)
            {
                // Heat the exhaust to combustion temperature
                exhaustMixture.Temperature = (exhaustMixture.Temperature * exhaustHeatCapacity + combustionEnergy) / exhaustHeatCapacity;
            }

            // Output exhaust ONLY to the tile atmosphere, never to pipe network
            // Use GetTileMixture to output to the grid tile, not the entity's container
            var tileMixture = _atmosphere.GetTileMixture(uid, excite: true);
            if (tileMixture != null)
                _atmosphere.Merge(tileMixture, exhaustMixture);
        }

        // Clear any remaining incompatible gases from internal mixture to prevent buildup
        foreach (var (incompatibleGas, _) in incompatibleGases)
        {
            internalMixture.AdjustMoles(incompatibleGas, -internalMixture.GetMoles(incompatibleGas));
        }

        // Sound indicator
        _ambientSound.SetAmbience(uid, supplier.MaxSupply > 100f);

        // Visual indicator - show active animation when consuming gas
        _appearance.SetData(uid, PowerDeviceVisuals.Powered, consumed > 0);
    }

    private void GeneratorPowerChange(EntityUid uid, GasGeneratorComponent component, ref PowerChangedEvent args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);
        if (!TryGetPipes(uid, out _))
        {
            // Disable if no pipes connected
        }
    }

    // Simplified model: legacy composition/temperature efficiency helpers removed.

    private bool TryGetPipes(EntityUid uid, out PipeNode? inlet)
    {
        inlet = null;

        if (!_nodeContainerQuery.TryGetComponent(uid, out var nodeContainer))
            return false;

        if (!nodeContainer.Nodes.TryGetValue(GasGeneratorComponent.NodeNameInlet, out var inletNode))
            return false;

        inlet = inletNode as PipeNode;

        return inlet != null;
    }

    /// <summary>
    /// Calculate composition-based modifiers for fuel gas mixture.
    /// Returns (baseEfficiency, powerMultiplier, fuelConsumptionMultiplier).
    /// Rich mixtures: More power but worse fuel economy.
    /// Lean mixtures: Less power but better fuel economy and hotter combustion.
    /// </summary>
    private (float efficiency, float powerMult, float fuelMult) CalculateCompositionEfficiency(GasMixture mixture, GasGeneratorComponent component)
    {
        var totalMoles = mixture.TotalMoles;
        if (totalMoles < 0.01f)
            return (0.5f, 0.8f, 1.0f); // Very lean mixture

        // Get mole fractions
        var primaryMoles = mixture.GetMoles(component.InputGas1);
        var primaryFraction = primaryMoles / totalMoles;

        // Calculate richness relative to optimal (negative = lean, positive = rich)
        var richness = primaryFraction - component.OptimalInputRatio;

        // Base efficiency: Gaussian penalty for deviation
        var deviation = Math.Abs(richness);
        var baseEfficiency = MathF.Exp(-(deviation * deviation) / (2 * 0.15f * 0.15f));
        baseEfficiency = MathHelper.Clamp(baseEfficiency, 0.0f, 1.0f); // Allow efficiency to drop to 0% for very poor mixtures

        // Rich mixture (excess fuel): More power, worse fuel economy
        // Lean mixture (excess oxidizer): Less power, better fuel economy
        float powerMultiplier;
        float fuelMultiplier;

        if (richness > 0.05f) // Rich
        {
            // Rich: Up to 20% more power, but up to 40% more fuel consumption
            var richnessFactor = MathF.Min(richness * 2f, 1.0f);
            powerMultiplier = 1.0f + (richnessFactor * 0.2f);
            fuelMultiplier = 1.0f + (richnessFactor * 0.4f);
        }
        else if (richness < -0.05f) // Lean
        {
            // Lean: Up to 20% less power, but up to 30% better fuel economy
            var leannessFactor = MathF.Min(Math.Abs(richness) * 2f, 1.0f);
            powerMultiplier = 1.0f - (leannessFactor * 0.2f);
            fuelMultiplier = 1.0f - (leannessFactor * 0.3f);
        }
        else // Near optimal
        {
            powerMultiplier = 1.0f;
            fuelMultiplier = 1.0f;
        }

        return (baseEfficiency, powerMultiplier, fuelMultiplier);
    }
}
