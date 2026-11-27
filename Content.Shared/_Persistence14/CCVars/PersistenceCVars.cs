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
    /// The name of the directory to store serialized SnapshotData from PhotoCamera
    /// </summary>
    public static readonly CVarDef<string> SnapshotPathPrefix = CVarDef.Create("ps14.snapshot_path_prefix",
        "/photo-snapshots/",
        CVar.SERVER | CVar.REPLICATED,
        desc: "The filepath prefix for serialized snapshots/photographs");

    /// <summary>
    /// Whether the PhotoCamera should compress serialized SnapshotData before saving, and uncompress before loading
    /// Any photos taken with the opposite setting will be unreadable.
    /// TODO: Implement
    /// </summary>
    public static readonly CVarDef<bool> SnapshotCompressData = CVarDef.Create("ps14.snapshot_compress_data",
        false,
        CVar.SERVER | CVar.REPLICATED,
        desc: "Whether the photo snapshot system should use compression. WARNING: Changing this will prevent existing photos from loading if they were taken in the opposite mode.");
}
