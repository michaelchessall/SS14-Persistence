using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.Shuttles;

[NetSerializable, Serializable]
public sealed partial class IFFUpdateEvent : EntityEventArgs
{
    public required NetEntity IffEnt;
    public Color? Color = null;
}