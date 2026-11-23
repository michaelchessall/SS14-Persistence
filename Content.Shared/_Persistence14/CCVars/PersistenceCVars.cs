using System.Runtime.InteropServices.Marshalling;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Persistence14.CCVars;

[CVarDefs]
public sealed partial class PersistenceCVars : CVars
{
    /// <summary>
    ///     The probability that a normal player's prayer is heard by the admins
    /// </summary>
    public static readonly CVarDef<float> NormalPrayerMessageChance = CVarDef.Create("ps14.normal_prayer_chance", 0.0f, CVar.SERVER | CVar.REPLICATED,
        desc: "The probability that a normal player's prayer is heard by the admins");

    /// <summary>
    ///     The probability that a player with the prayer insight is heard by the admins.
    /// </summary>
    public static readonly CVarDef<float> InsightPrayerMessageChance = CVarDef.Create("ps14.insight_prayer_chance", 1.0f, CVar.SERVER | CVar.REPLICATED,
        desc: "The probability that a player with the prayer insight is heard by the admins.");

    /// <summary>
    /// The maximum multiplier research entropy can increase research costs by.
    /// </summary>
    public static readonly CVarDef<float> MaxEntropyResearchMultiplier = CVarDef.Create("ps14.entropy_multiplier_max", 2.0f, CVar.SERVER | CVar.REPLICATED,
        desc: "The maximum multiplier research entropy can increase research costs by.");

    /// <summary>
    /// The exponential used to scale the entropy calculation.
    /// Larger values result in less penalty for low levels of diversity and more for higher levels of diversity.
    /// </summary>
    public static readonly CVarDef<float> EntropyScalingCoefficient = CVarDef.Create("ps14.entropy_scaling_coeff", 5.0f, CVar.SERVER | CVar.REPLICATED,
        desc: "The exponential used to scale the entropy calculation. Larger values result in less penalty for low levels of diversity and more for higher levels of diversity.");
}
