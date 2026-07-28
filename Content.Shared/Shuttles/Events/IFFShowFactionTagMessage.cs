using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

[Serializable, NetSerializable]
public sealed class IFFShowFactionTagMessage : BoundUserInterfaceMessage
{
    public bool Show;
}
