using Content.Client.Administration.Managers;
using Content.Client.CrewAssignments.UI;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.CCVar;
using Content.Shared.CrewMetaRecords;
using Content.Shared.IdentityManagement;
using Content.Shared.MessageBoard.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using static Robust.Client.UserInterface.Controls.MenuBar;

namespace Content.Client.MessageBoard.UI;

[UsedImplicitly]
public sealed class MessageBoardBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private MessageBoard? _menu;
    private CreateEntry? _createEntry;
    private EntryWindow? _entryWindow;
    private ConversationWindow? _conversationWindow;

    private Dictionary<string, DirectMessageConversation>? _directMessages = null;

    public MessageBoardBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        bool isAdmin = _admin.IsActive();
        var player = PlayerManager.LocalEntity;
        if (player == null) return;
        var playerName = Identity.Name(player.Value, EntMan);
        base.UpdateState(state);
        if(_menu == null)
        {
            return;
        }
        if (state is not MessageBoardInterfaceState cState)
            return;
        _menu.PublicBoardEntriesBC.RemoveAllChildren();
        cState.PublicEntries.Reverse();
        foreach (var entry in cState.PublicEntries)
        {
            bool delete = false;
            if (isAdmin || playerName == entry.Author) delete = true;
            var adjustedTime = entry.CreationTime.AddYears(_cfg.GetCVar(CCVars.YearOffset));
            EntryHeader header = new EntryHeader(entry.Title, entry.Author, $"{adjustedTime.ToString()}", entry.Comments.Count, delete);
            _menu.PublicBoardEntriesBC.AddChild(header);
            header.DeleteButton.OnPressed += (args) =>
            {
                SendMessage(new MessageBoardDeleteEntryPublicMessage(entry.UID));
            };
            header.ViewButton.OnPressed += (args) =>
            {
                if(_entryWindow != null)
                {
                    _entryWindow.Dispose();
                }
                var entryWindow = new EntryWindow(entry, isAdmin, playerName, this);
                _entryWindow = entryWindow;
                entryWindow.OpenCentered();
                entryWindow.AddCommentBtn.OnPressed += (commentArgs) =>
                {
                    var comment = entryWindow.AddCommentLE.Text;
                    if (comment == string.Empty) return;
                    var ID = entryWindow.Entry.UID;
                    entryWindow.AddCommentLE.Text = string.Empty;
                    SendMessage(new MessageBoardPostCommentPublicMessage(ID, comment));
                };
            };
            if (_entryWindow != null && _entryWindow.Entry.UID == entry.UID)
            {
                _entryWindow.UpdateEntry(entry);
            }
        }

        _menu.ActiveConversations.RemoveAllChildren();
        if (_directMessages != null)
        {
            foreach (var kv in _directMessages)
            {
                var author = kv.Key;
                var conversation = kv.Value;
                Button convoButton = new Button();
                convoButton.Text = author;
                convoButton.OnPressed += (args) =>
                {
                    if (_conversationWindow != null)
                    {
                        _conversationWindow.Dispose();
                    }
                    var conversationWindow = new ConversationWindow(conversation, isAdmin, playerName, author);
                    _conversationWindow = conversationWindow;
                    conversationWindow.OpenCentered();
                    conversationWindow.SendBtn.OnPressed += (sendArgs) =>
                    {
                        var message = conversationWindow.SendLE.Text;
                        if (message == string.Empty) return;
                        var recipient = conversationWindow.RecipientName;
                        conversationWindow.SendLE.Text = string.Empty;
                        SendMessage(new MessageBoardSendDirectMessagePublicMessage(recipient, message));
                    };
                };
                if (_conversationWindow != null && _conversationWindow.RecipientName == kv.Key)
                {
                    _conversationWindow.UpdateEntry(kv.Value);
                }
                _menu.ActiveConversations.AddChild(convoButton);
            }
        }

        _menu.SendMessageButton.OnPressed += (args) =>
        {
            if (_conversationWindow != null)
            {
                _conversationWindow.Dispose();
            }
            ConversationWindow? conversationWindow;

            if(_directMessages != null && _directMessages.ContainsKey(_menu.SendMessageLE.Text))
            {
                var conversation = _directMessages[_menu.SendMessageLE.Text];
                conversationWindow = new ConversationWindow(conversation, isAdmin, playerName, _menu.SendMessageLE.Text);
            }
            else
            {
                conversationWindow = new ConversationWindow(new DirectMessageConversation(), isAdmin, playerName, _menu.SendMessageLE.Text);
            }
            if(conversationWindow != null)
            {
                _conversationWindow = conversationWindow;
                conversationWindow.OpenCentered();
                conversationWindow.SendBtn.OnPressed += (sendArgs) =>
                {
                    var message = conversationWindow.SendLE.Text;
                    if (message == string.Empty) return;
                    var recipient = conversationWindow.RecipientName;
                    conversationWindow.SendLE.Text = string.Empty;
                    SendMessage(new MessageBoardSendDirectMessagePublicMessage(recipient, message));
                };

            }
        };


    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (_menu == null) return;
        if (message is not MessageBoardUpdateDirectMessagesMessage updateMessage) return;
        UpdateDirectMessages(updateMessage);
    }

    private void UpdateDirectMessages(MessageBoardUpdateDirectMessagesMessage updateMessage)
    {
        var player = PlayerManager.LocalEntity;
        if (player == null) return;
        var playerName = Identity.Name(player.Value, EntMan);
        bool isAdmin = _admin.IsActive();
        if (_menu == null) return;
        _directMessages = updateMessage.DirectMessages;
        _menu.ActiveConversations.RemoveAllChildren();
        foreach (var kv in _directMessages)
        {
            var author = kv.Key;
            var conversation = kv.Value;
            Button convoButton = new Button();
            convoButton.Text = author;
            convoButton.OnPressed += (args) =>
            {
                if (_conversationWindow != null)
                {
                    _conversationWindow.Dispose();
                }
                var conversationWindow = new ConversationWindow(conversation, isAdmin, playerName, author);
                _conversationWindow = conversationWindow;
                conversationWindow.OpenCentered();
            };
            if (_conversationWindow != null && _conversationWindow.RecipientName == kv.Key)
            {
                _conversationWindow.UpdateEntry(kv.Value);
            }
            _menu.ActiveConversations.AddChild(convoButton);
        }
    }

    protected override void Open()
    {
        base.Open();
        var spriteSystem = EntMan.System<SpriteSystem>();
        var dependencies = IoCManager.Instance!;
        _menu = new MessageBoard(Owner, EntMan, dependencies.Resolve<IPrototypeManager>(), spriteSystem);
        var localPlayer = dependencies.Resolve<IPlayerManager>().LocalEntity;
        var description = new FormattedMessage();
        _menu.OnClose += Close;
        _menu.OpenCentered();
        _menu.CreateEntryPublicButton.OnPressed += CreateEntryPublic;
    }

    public void CreateEntryPublic(BaseButton.ButtonEventArgs args)
    {
        if(_createEntry != null)
        {
            _createEntry.Dispose();
        }
        _createEntry = new CreateEntry();
        _createEntry.OpenCentered();
        _createEntry.CurrentEntryType = CreateEntry.EntryType.Public;
        _createEntry.PostButton.OnPressed += FinalizeEntryPublic;
    }

    public void FinalizeEntryPublic(BaseButton.ButtonEventArgs args)
    {
        if (_createEntry == null)
            return;
        var title = _createEntry.MainTitleLabel.Text;
        var content = Rope.Collapse(_createEntry.DescriptionLabel.TextRope);
        if(title == string.Empty || content == string.Empty)
        {
            return;
        }
        SendMessage(new MessageBoardCreateEntryPublicMessage(title, content));
        _createEntry.Dispose();
        _createEntry = null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }

}
