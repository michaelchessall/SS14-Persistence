using Content.Shared.CrewAccesses.Components;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.CrewAssignments.Prototypes;
using Content.Shared.Radio;
using Content.Shared.Station.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.MessageBoard.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MessageBoardComponent : Component
{
}


[DataDefinition]
[Serializable]
[Virtual]
public partial class MessageBoardEntry
{
    public int UID;
    public string Title;
    public string Author;
    public string Body;
    public DateTime CreationTime;
    public List<MessageBoardComment> Comments = new();
    public int NextCommentID = 0;

    public MessageBoardEntry(int uid, string title, string author, string body)
    {
        UID = uid;
        Title = title;
        Author = author;
        Body = body;
        CreationTime = DateTime.Now;
    }

}

[DataDefinition]
[Serializable]
[Virtual]
public partial class MessageBoardComment
{
    public int UID;
    public string Author;
    public string Body;
    public DateTime CreationTime;

    public MessageBoardComment(int uid, string author, string body)
    {
        UID = uid;
        Author = author;
        Body = body;
        CreationTime = DateTime.Now;
    }

}

[NetSerializable, Serializable]
public sealed class MessageBoardInterfaceState : BoundUserInterfaceState
{
    public List<MessageBoardEntry> PublicEntries;

    public MessageBoardInterfaceState(List<MessageBoardEntry> publicEntries)
    {
        PublicEntries = publicEntries;
    }
}

[Serializable, NetSerializable]
public sealed class MessageBoardCreateEntryPublicMessage : BoundUserInterfaceMessage
{
    public string Title;
    public string Body;

    public MessageBoardCreateEntryPublicMessage(string title, string body)
    {
        Title = title;
        Body = body;
    }
}

[Serializable, NetSerializable]
public sealed class MessageBoardPostCommentPublicMessage : BoundUserInterfaceMessage
{
    public int EntryId;
    public string Body;

    public MessageBoardPostCommentPublicMessage(int entryId, string body)
    {
        EntryId = entryId;
        Body = body;
    }
}

[Serializable, NetSerializable]
public sealed class MessageBoardDeleteCommentPublicMessage : BoundUserInterfaceMessage
{
    public int CommentId;
    public int EntryId;


    public MessageBoardDeleteCommentPublicMessage(int commentId, int entryId)
    {
        CommentId = commentId;
        EntryId = entryId;
    }
}

[Serializable, NetSerializable]
public sealed class MessageBoardDeleteEntryPublicMessage : BoundUserInterfaceMessage
{
    public int EntryId;
    public MessageBoardDeleteEntryPublicMessage(int entryId)
    {
        EntryId = entryId;
    }
}



