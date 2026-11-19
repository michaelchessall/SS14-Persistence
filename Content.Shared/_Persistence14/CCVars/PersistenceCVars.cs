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
}
