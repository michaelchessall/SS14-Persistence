using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Persistence14.Admin;

[RegisterComponent]
public sealed partial class LoocMuteManagerComponent : Component
{
    [DataField]
    public HashSet<NetUserId> PermanentMuteIds = new();

    [DataField]
    public Dictionary<NetUserId, PlayerMuteDuration> MuteDurations = new();
}

[DataDefinition]
public sealed partial class PlayerMuteDuration
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Duration;

    public static implicit operator TimeSpan(PlayerMuteDuration duration) => duration.Duration;
    public static implicit operator PlayerMuteDuration(TimeSpan timespan) => new PlayerMuteDuration() { Duration = timespan };
}