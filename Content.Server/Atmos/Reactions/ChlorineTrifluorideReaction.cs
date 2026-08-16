using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    /// <summary>
    /// ClF3 oxidation: ClF3 violently oxidizes any reactive gas it contacts.
    /// Hypergolic — reacts on contact at room temperature, no ignition source needed.
    ///
    /// Per mole of ClF3 consumed, always produces 0.5 mol Cl₂ + 1.5 mol F₂ (toxic byproducts).
    /// Additional oxidation products vary by target:
    ///   WaterVapor → destroyed (2 ClF₃ per 3 H₂O)
    ///   Hydrogen   → destroyed (ClF₃ per 1.5 H₂)
    ///   Ammonia    → Nitrogen  (ClF₃ per NH₃, yields 0.5 N₂)
    ///   Methane    → CO₂       (4 ClF₃ per 3 CH₄, yields 1 CO₂ per CH₄)
    ///   Plasma     → CO₂       (ClF₃ per Plasma, yields 0.5 CO₂)
    ///
    /// Creates a hotspot on the tile representing the violent fire caused by oxidation.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ChlorineTrifluorideReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {
            return ReactionResult.NoReaction;
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;
            mixture.ReactionResults[(byte)GasReaction.Fire] = 0;

            var initialClF3 = mixture.GetMoles(Gas.ChlorineTrifluoride);
            if (initialClF3 < Atmospherics.GasMinMoles)
                return ReactionResult.NoReaction;

            var initialTritium = mixture.GetMoles(Gas.Tritium);


            // Use hotspot temperature if available.
            var reactionTemperature = temperature;
            if (location?.Hotspot.Valid == true)
                reactionTemperature = location.Hotspot.Temperature;

            // ClF3 is hypergolic — scales from slow at room temp to full rate at 300°C.
            var temperatureScale = 0f;
            if (reactionTemperature > Atmospherics.ClF3OxidationUpperTemperature)
                temperatureScale = 1f;
            else if (reactionTemperature > Atmospherics.ClF3OxidationMinTemperature)
                temperatureScale = (reactionTemperature - Atmospherics.ClF3OxidationMinTemperature) /
                                   (Atmospherics.ClF3OxidationUpperTemperature - Atmospherics.ClF3OxidationMinTemperature);

            if (temperatureScale <= 0f)
                return ReactionResult.NoReaction;

            // Maximum ClF3 that can react this tick (for normal targets).
            var remainingClF3 = initialClF3 * Atmospherics.ClF3OxidationRate * temperatureScale;
            var totalClF3Consumed = 0f;

            // Tritium is consumed FIRST with a separate, much larger budget.
            // ClF3 + Tritium is fully hypergolic — no temperature scaling.
            // Consumes up to 80% of available ClF3 per tick on tritium alone.
            var tritiumBudget = initialClF3 * 0.8f;
            var tritiumClF3Used = OxidizeGas(mixture, Gas.Tritium, ref tritiumBudget,
                clf3PerTarget: 1f, product: Gas.WaterVapor, productPerTarget: 0.5f);
            // Tritium oxidation also produces Hydrogen as a byproduct.
            if (tritiumClF3Used > 0f)
                mixture.AdjustMoles(Gas.Hydrogen, tritiumClF3Used * 0.5f);
            totalClF3Consumed += tritiumClF3Used;
            // Reduce the normal budget by whatever was used on tritium.
            remainingClF3 = MathF.Max(0f, remainingClF3 - tritiumClF3Used);

            // ClF3 oxidizes remaining target gases in priority order.
            // WaterVapor: 2 ClF₃ + 3 H₂O → byproducts. ClF3 per H₂O = 2/3.
            totalClF3Consumed += OxidizeGas(mixture, Gas.WaterVapor, ref remainingClF3,
                clf3PerTarget: 2f / 3f, product: null, productPerTarget: 0f);

            // Hydrogen: ClF₃ + 1.5 H₂ → byproducts. ClF3 per H₂ = 1/1.5.
            totalClF3Consumed += OxidizeGas(mixture, Gas.Hydrogen, ref remainingClF3,
                clf3PerTarget: 1f / 1.5f, product: null, productPerTarget: 0f);

            // Ammonia: ClF₃ + NH₃ → 0.5 N₂. ClF3 per NH₃ = 1.
            totalClF3Consumed += OxidizeGas(mixture, Gas.Ammonia, ref remainingClF3,
                clf3PerTarget: 1f, product: Gas.Nitrogen, productPerTarget: 0.5f);

            // Methane: 4 ClF₃ + 3 CH₄ → 3 CO₂. ClF3 per CH₄ = 4/3.
            totalClF3Consumed += OxidizeGas(mixture, Gas.Methane, ref remainingClF3,
                clf3PerTarget: 4f / 3f, product: Gas.CarbonDioxide, productPerTarget: 1f);

            // Plasma: ClF₃ + Plasma → 0.5 CO₂. ClF3 per Plasma = 1.
            totalClF3Consumed += OxidizeGas(mixture, Gas.Plasma, ref remainingClF3,
                clf3PerTarget: 1f, product: Gas.CarbonDioxide, productPerTarget: 0.5f);

            if (totalClF3Consumed > Atmospherics.GasMinMoles)
            {
                // Remove consumed ClF3 and produce toxic byproducts.
                mixture.AdjustMoles(Gas.ChlorineTrifluoride, -totalClF3Consumed);
                mixture.AdjustMoles(Gas.Chlorine, totalClF3Consumed * 0.5f);
                mixture.AdjustMoles(Gas.Fluorine, totalClF3Consumed * 1.5f);

                // Base oxidation energy for non-tritium targets.
                var nonTritiumClF3 = totalClF3Consumed - tritiumClF3Used;
                energyReleased = Atmospherics.ClF3OxidationEnergy * nonTritiumClF3;

                // ClF3 + Tritium is far more energetic — radiative flash.
                energyReleased += Atmospherics.ClF3TritiumEnergy * tritiumClF3Used;

                energyReleased /= heatScale;
                mixture.ReactionResults[(byte)GasReaction.Fire] = totalClF3Consumed;
            }

            if (energyReleased > 0f)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;
            }

            // ClF3 oxidation causes fire on the tile.
            if (location != null && totalClF3Consumed > Atmospherics.GasMinMoles)
            {
                atmosphereSystem.HotspotExpose(location, mixture.Temperature, mixture.Volume);
            }

            // ClF3 corrodes items on contact regardless of whether gases were consumed.
            // Even without target gases, ClF3 vapour attacks nearby materials.
            if (location != null && temperatureScale > 0f)
            {
                atmosphereSystem.PerformClF3Exposure(location, initialClF3, reactionTemperature);
            }

            // ClF3 + Tritium produces a radiation pulse — the fluorination of tritium
            // releases intense ionizing radiation as a radiative flash.
            // Pass the INITIAL tritium on the tile, not just the consumed amount,
            // so the visual/radiation scales with the actual tritium present.
            if (location != null && tritiumClF3Used > Atmospherics.GasMinMoles)
            {
                atmosphereSystem.SpawnRadiationPulse(location, initialTritium);
            }

            if (totalClF3Consumed <= Atmospherics.GasMinMoles)
                return ReactionResult.NoReaction;

            // When ClF3 oxidizes tritium, it dominates all chemistry on the tile.
            // Prevent TritiumFire from competing for the remaining tritium.
            if (tritiumClF3Used > Atmospherics.GasMinMoles)
                return ReactionResult.Reacting | ReactionResult.StopReactions;

            return ReactionResult.Reacting;
        }

        /// <summary>
        /// Attempts to oxidize a target gas with ClF3. Consumes the target and deducts from
        /// the remaining ClF3 budget. Returns the amount of ClF3 consumed.
        /// </summary>
        private static float OxidizeGas(
            GasMixture mixture,
            Gas target,
            ref float remainingClF3,
            float clf3PerTarget,
            Gas? product,
            float productPerTarget)
        {
            if (remainingClF3 < Atmospherics.GasMinMoles)
                return 0f;

            var targetMoles = mixture.GetMoles(target);
            if (targetMoles < Atmospherics.GasMinMoles)
                return 0f;

            // How much target can we oxidize with remaining ClF3?
            var maxTargetByClF3 = remainingClF3 / clf3PerTarget;
            var targetConsumed = MathF.Min(targetMoles, maxTargetByClF3);
            var clf3Consumed = targetConsumed * clf3PerTarget;

            mixture.AdjustMoles(target, -targetConsumed);
            if (product.HasValue && productPerTarget > 0f)
                mixture.AdjustMoles(product.Value, targetConsumed * productPerTarget);

            remainingClF3 -= clf3Consumed;
            return clf3Consumed;
        }
    }
}
