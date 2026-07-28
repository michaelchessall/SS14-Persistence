using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

[Serializable, NetSerializable]
public sealed class StationModificationChangeFactionTag : BoundUserInterfaceMessage
{
    public string Tag;

    public StationModificationChangeFactionTag(string tag)
    {
        Tag = tag;
    }
}
