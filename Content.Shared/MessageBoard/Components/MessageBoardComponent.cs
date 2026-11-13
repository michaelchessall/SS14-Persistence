using Content.Shared.CrewAccesses.Components;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.CrewAssignments.Prototypes;
using Content.Shared.CrewMetaRecords;
using Content.Shared.MessageBoard.Prototypes;
using Content.Shared.Radio;
using Content.Shared.Station.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.MessageBoard.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MessageBoardComponent : Component
{
    [DataField]
    public bool Silenced = false;
}


[DataDefinition]
[Serializable]
[Virtual]
public partial class MessageBoardEntry
{
    [DataField]
    public int UID;
    [DataField]
    public string Title;
    [DataField]
    public string Author;
    [DataField]
    public string Body;
    [DataField]
    public DateTime CreationTime;
    [DataField]
    public List<MessageBoardComment> Comments = new();
    [DataField]
    public int NextCommentID = 0;
    [DataField]
    public ProtoId<MessageBoardCategoryPrototype> Category = "General";

    public MessageBoardEntry(int uid, string title, string author, string body, ProtoId<MessageBoardCategoryPrototype> categoryProtoID)
    {
        UID = uid;
        Title = title;
        Author = author;
        Body = body;
        CreationTime = DateTime.Now;
        Category = categoryProtoID;
    }

}

[DataDefinition]
[Serializable]
[Virtual]
public partial class MessageBoardComment
{
    [DataField]
    public int UID;
    [DataField]
    public string Author;
    [DataField]
    public string Body;
    [DataField]
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
    public bool Silenced;
    public MessageBoardInterfaceState(List<MessageBoardEntry> publicEntries, bool silenced)
    {
        PublicEntries = publicEntries;
        Silenced = silenced;
    }
}
[Serializable, NetSerializable]
public sealed class MessageBoardUpdateDirectMessagesMessage : BoundUserInterfaceMessage
{
    public Dictionary<string, DirectMessageConversation> DirectMessages;
    public MessageBoardUpdateDirectMessagesMessage(Dictionary<string, DirectMessageConversation> directMessages)
    {
        DirectMessages = directMessages;
    }
}


[Serializable, NetSerializable]
public sealed class MessageBoardCreateEntryPublicMessage : BoundUserInterfaceMessage
{
    public string Title;
    public string Body;
    public string CategoryProtoID;

    public MessageBoardCreateEntryPublicMessage(string title, string body, string iD)
    {
        Title = title;
        Body = body;
        CategoryProtoID = iD;
    }
}


[Serializable, NetSerializable]
public sealed class MessageBoardSendDirectMessagePublicMessage : BoundUserInterfaceMessage
{
    public string Recipient;
    public string Body;
    public MessageBoardSendDirectMessagePublicMessage(string recipient, string body)
    {
        Recipient = recipient;
        Body = body;
    }
}


[Serializable, NetSerializable]
public sealed class MessageBoardSilenceMessage : BoundUserInterfaceMessage
{
    public bool State;
    public MessageBoardSilenceMessage(bool state)
    {
        State = state;
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



