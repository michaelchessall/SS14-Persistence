using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.CrewRecords.Systems;
using Content.Shared.MessageBoard.Components;
using Content.Shared.MessageBoard.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.MessageBoard.Systems;

public sealed partial class MessageBoardSystem : SharedMessageBoardSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly CrewMetaRecordsSystem _crewMetaRecordsSystem = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MessageBoardComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<MessageBoardComponent, MessageBoardCreateEntryPublicMessage>(OnCreateEntryPublic);
        SubscribeLocalEvent<MessageBoardComponent, MessageBoardPostCommentPublicMessage>(OnPostCommentPublic);
        SubscribeLocalEvent<MessageBoardComponent, MessageBoardDeleteCommentPublicMessage>(OnDeleteCommentPublic);
        SubscribeLocalEvent<MessageBoardComponent, MessageBoardDeleteEntryPublicMessage>(OnDeleteEntryPublic);
    }

    private void OnDeleteEntryPublic(Entity<MessageBoardComponent> ent, ref MessageBoardDeleteEntryPublicMessage args)
    {
        bool isAdmin = _adminManager.IsAdmin(args.Actor);

        MessageBoardEntry? entry = null;
        var metaRecord = _crewMetaRecordsSystem.MetaRecords;
        if (metaRecord == null) return;
        foreach (var e in metaRecord.MessageBoardEntries)
        {
            if (e.UID == args.EntryId)
            {
                entry = e;
                break;
            }
        }
        if (entry == null) return;
        if (isAdmin || entry.Author == Name(args.Actor))
        {
            metaRecord.MessageBoardEntries.Remove(entry);
            UpdateAllUserInterfaces();
            return;
        }
    }

    private void OnDeleteCommentPublic(Entity<MessageBoardComponent> ent, ref MessageBoardDeleteCommentPublicMessage args)
    {
        bool isAdmin = _adminManager.IsAdmin(args.Actor);

        MessageBoardEntry? entry = null;
        var metaRecord = _crewMetaRecordsSystem.MetaRecords;
        if (metaRecord == null) return;
        foreach (var e in metaRecord.MessageBoardEntries)
        {
            if (e.UID == args.EntryId)
            {
                entry = e;
                break;
            }
        }
        if (entry == null) return;
        foreach (var c in entry.Comments)
        {
            if (c.UID == args.CommentId)
            {
                if (isAdmin || c.Author == Name(args.Actor))
                {
                    entry.Comments.Remove(c);
                    UpdateAllUserInterfaces();
                    return;
                }
            }
        }
    }

    private void OnPostCommentPublic(Entity<MessageBoardComponent> ent, ref MessageBoardPostCommentPublicMessage args)
    {
        bool isAdmin = _adminManager.IsAdmin(args.Actor);
        MessageBoardEntry? entry = null;
        var metaRecord = _crewMetaRecordsSystem.MetaRecords;
        if (metaRecord == null) return;
        metaRecord.TryGetRecord(Name(args.Actor), out var authorRecord);
        if (authorRecord == null) return;
        TimeSpan diff = authorRecord.NextMessageBoardComment - _timing.CurTime;
        if (!isAdmin && authorRecord.NextMessageBoardComment > _timing.CurTime)
        {
            if (TryComp<ActorComponent>(args.Actor, out var actor) && actor != null && actor.PlayerSession != null)
            {
                var msg = $"You cannot post comments so quickly. Wait another {diff.TotalSeconds:F1} seconds";
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Notifications,
                    msg,
                    msg,
                    args.Actor,
                    false,
                    actor.PlayerSession.Channel
                    );
            }
            return;
        }
        foreach (var e in metaRecord.MessageBoardEntries)
        {
            if (e.UID == args.EntryId)
            {
                entry = e;
                break;
            }
        }
        if (entry == null) return;
        MessageBoardComment newComment = new(entry.NextCommentID++, Name(args.Actor), args.Body);
        entry.Comments.Add(newComment);
        authorRecord.NextMessageBoardComment = _timing.CurTime + TimeSpan.FromSeconds(5);
        UpdateAllUserInterfaces();
    }

    public void UpdateAllUserInterfaces()
    {
        var query = EntityQueryEnumerator<MessageBoardComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            UpdateUserInterface(uid, component);
        }
    }

    private void OnUIOpened(Entity<MessageBoardComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent.Owner, ent.Comp);
    }

    private void OnCreateEntryPublic(Entity<MessageBoardComponent> ent, ref MessageBoardCreateEntryPublicMessage args)
    {
        bool isAdmin = _adminManager.IsAdmin(args.Actor);
        var metaRecord = _crewMetaRecordsSystem.MetaRecords;
        if (metaRecord == null) return;
        metaRecord.TryGetRecord(Name(args.Actor), out var authorRecord);
        if (authorRecord == null) return;
        TimeSpan diff = authorRecord.NextMessageBoardEntry - _timing.CurTime;
        if (!isAdmin && authorRecord.NextMessageBoardEntry > _timing.CurTime)
        {
            if (TryComp<ActorComponent>(args.Actor, out var actor) && actor != null && actor.PlayerSession != null)
            {
                var msg = $"You cannot create entries so quickly. Wait another {diff.TotalMinutes:F1} minutes";
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Notifications,
                    msg,
                    msg,
                    args.Actor,
                    false,
                    actor.PlayerSession.Channel
                    );
            }
            return;
        }
        MessageBoardEntry newEntry = new(metaRecord.NextMessageBoardEntryID++, args.Title, Name(args.Actor), args.Body);
        metaRecord.MessageBoardEntries.Add(newEntry);
        authorRecord.NextMessageBoardEntry = _timing.CurTime + TimeSpan.FromHours(4);
        UpdateAllUserInterfaces();
    }

    private void UpdateUserInterface(EntityUid uid, MessageBoardComponent component)
    {
        var metaRecord = _crewMetaRecordsSystem.MetaRecords;
        if (metaRecord == null) return;
        var entries = metaRecord.MessageBoardEntries;
        _uiSystem.SetUiState(uid, MessageBoardUiKey.Main, new MessageBoardInterfaceState(entries));
    }
}
