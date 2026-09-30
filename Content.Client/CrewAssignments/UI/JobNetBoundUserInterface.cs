using Content.Client.CrewAssignments.UI;
using Content.Client.MessageBoard.UI;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Shared.Cargo.Components;
using Content.Shared.CrewAssignments;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.Store;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.OptionButton;

namespace Content.Client.Store.Ui;

[UsedImplicitly]
public sealed class JobNetBoundUserInterface : BoundUserInterface
{

    [ViewVariables]
    private JobNetMenu? _menu;

    [ViewVariables]
    public CodexEntryMenu? CodexMenu;

    public ConversationWindow? ConversationWindow;

    public JobNetBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        var spriteSystem = EntMan.System<SpriteSystem>();
        _menu = this.CreateWindow<JobNetMenu>();
        _menu.Owner = this;
        _menu._spriteSystem = spriteSystem;
        _menu.PossibleJobs.OnItemSelected += OnJobPressed;
        _menu.LevelPurchaseButton.OnPressed += OnLevelPurchase;
        CodexMenu = new();
    }

    public void OnLevelPurchase(ButtonEventArgs args)
    {
        SendMessage(new JobNetPurchaseMessage());
    }

    public void OnJobPressed(ItemSelectedEventArgs args)
    {
        SendMessage(new JobNetSelectMessage(args.Id));
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_menu == null) return;
        if (state is not JobNetUpdateState cState)
            return;
        _menu.UpdateState(cState);


    }

    public void CancelRumor(int ind)
    {
        SendMessage(new JobNetCancelRumorMessage(ind));
    }

    internal void TransferRumor(int rumorIndex, string text)
    {
        SendMessage(new JobNetTransferRumorMessage(rumorIndex, text));
    }
}
