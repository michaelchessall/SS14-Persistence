using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Components._Persistence14;

[Serializable, NetSerializable]
public enum BookPublisherUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BookPublisherBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool BookInserted;
    public readonly string BookName;
    public readonly bool CanPublish;
    public readonly string Title;

    public BookPublisherBoundUserInterfaceState(bool bookInserted, string bookName, bool canPublish, string title)
    {
        BookInserted = bookInserted;
        BookName = bookName;
        CanPublish = canPublish;
        Title = title;
    }
}

[Serializable, NetSerializable]
public sealed class BookPublisherPublishMessage : BoundUserInterfaceMessage
{
    public readonly string Title;

    public BookPublisherPublishMessage(string title)
    {
        Title = title;
    }
}

[Serializable, NetSerializable]
public sealed class BookPublisherEjectMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class BookPublisherRefreshMessage : BoundUserInterfaceMessage
{
}
