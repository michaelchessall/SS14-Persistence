using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.Atmos.Geyser;

[NetSerializable, Serializable]
public sealed class GasGeyserErruptedEvent : EntityEventArgs
{
    public NetEntity Geyser { get; }

    public GasGeyserErruptedEvent(NetEntity geyser)
    {
        Geyser = geyser;
    }
}