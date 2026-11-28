using System.Runtime.InteropServices.Marshalling;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Persistence14.CCVars;

[CVarDefs]
public sealed partial class PersistenceCVars : CVars
{
    /// <summary>
    /// Added to the current year (server time) for IC references to the current year
    /// </summary>
    public static readonly CVarDef<int>
        YearOffset = CVarDef.Create("ps14.lore.year_offset", 120, CVar.ARCHIVE);

    /// <summary>
    /// How long in seconds you must wait after death before you can PK your character
    /// </summary>
    public static readonly CVarDef<int>
        AcceptDeathTime = CVarDef.Create("ps14.deathnet.accept_death_time", 60 * 30, CVar.ARCHIVE);

    /// <summary>
    /// Cooldown time in seconds for broadcasting an SOS message while dead
    /// </summary>
    public static readonly CVarDef<int>
        SOSCooldown = CVarDef.Create("ps14.deathnet.sos_time", 60 * 30, CVar.ARCHIVE);

    /// <summary>
    /// Unused
    /// </summary>
    public static readonly CVarDef<int>
        GridClaimPersonalMaxTiles = CVarDef.Create("ps14.gridconfig.claim_personal_max_tiles", 150, CVar.ARCHIVE);

    #region Bluespace Parking

    /// <summary>
    /// Maximum number of tiles before a grid can no longer be bluespace parked
    /// </summary>
    public static readonly CVarDef<int>
        BluespaceParkingMaxTiles = CVarDef.Create("ps14.bsp.max_tiles", 800, CVar.ARCHIVE);

    /// <summary>
    /// How long it takes to bluespace park a grid.
    /// </summary>
    public static readonly CVarDef<int>
        BluespaceParkingParkDelay = CVarDef.Create("ps14.bsp.park_delay", 25, CVar.ARCHIVE);

    /// <summary>
    /// How long it takes to bluespace unpark a grid.
    /// </summary>
    public static readonly CVarDef<int>
        BluespaceParkingUnparkDelay = CVarDef.Create("ps14.bsp.unpark_delay", 25, CVar.ARCHIVE);

    /// <summary>
    /// How far from the original parking location unparking is permitted.
    /// </summary>
    public static readonly CVarDef<float>
        BluespaceUnparkMaxDistance = CVarDef.Create("ps14.bsp.unpark_max_distance", 100f, CVar.ARCHIVE);

    /// <summary>
    /// The subdirectory to save bluespace parking grids into.
    /// </summary>
    public static readonly CVarDef<string>
        BluespaceParkingDirectory = CVarDef.Create("ps14.bsp.save_dir", "BluespaceParking", CVar.SERVERONLY);

    #endregion Bluespace Parking

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

    #region Science

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

    #endregion Science
}
