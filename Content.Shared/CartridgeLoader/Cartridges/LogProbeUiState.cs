using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class LogProbeUiState : BoundUserInterfaceState
{
    /// <summary>
    /// The name of the scanned entity.
    /// </summary>
    public string EntityName;

    /// <summary>
    /// The list of probed network devices
    /// </summary>
    public List<PulledAccessLog> PulledLogs;

    public LogProbeUiState(string entityName, List<PulledAccessLog> pulledLogs)
    {
        EntityName = entityName;
        PulledLogs = pulledLogs;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed partial class PulledAccessLog
{
    public readonly DateTime Time;
    public readonly string Accessor;

    public PulledAccessLog(DateTime time, string accessor)
    {
        Time = time;
        Accessor = accessor;
    }
}
