using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.Atmos;

[NetSerializable, Serializable]
public sealed class WallAtmosLeakEvent : EntityEventArgs
{
    public NetEntity Entity { get; }
    public bool Leaking { get; }

    public WallAtmosLeakEvent(NetEntity entity, bool leaking)
    {
        Entity = entity;
        Leaking = leaking;
    }
}
