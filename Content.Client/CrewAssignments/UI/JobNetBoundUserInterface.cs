using Content.Client.CrewAssignments.UI;
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
        _menu.DealerSelect.OnPressed += DealerSelect_OnPressed;
        _menu.AssassinSelect.OnPressed += AssassinSelect_OnPressed;
        _menu.BountyHSelect.OnPressed += BountyHSelect_OnPressed;
        _menu.HuntedLEB.OnPressed += HuntedLEB_OnPressed;
        _menu.HuntLEB.OnPressed += HuntLEB_OnPressed;
        _menu.OnItemSelected += (row) =>
        {
            if (row == null || row.Product == null)
                return;

            SendMessage(new JobNetPurchasePrecursorMessage(row.Product.ID));
        };
        _menu.OnLabelButtonPressed += id =>
        {
            SendMessage(new JobNetDealerLabelMessage(id));
        };
        CodexMenu = new();
        _menu.PrecursorGuidebook.OnPressed += (ButtonEventArgs obj) =>
        {
            var guidebookController = _menu.UserInterfaceManager.GetUIController<GuidebookUIController>();
            guidebookController.OpenGuidebook(selected: "Precursor");
        };
    }

    private void HuntLEB_OnPressed(ButtonEventArgs obj)
    {
        if (_menu == null) return;
        SendMessage(new JobNetSubmitHuntMessage(_menu.HuntLE.Text));
    }

    private void HuntedLEB_OnPressed(ButtonEventArgs obj)
    {
        if (_menu == null) return;
        SendMessage(new JobNetSubmitHuntedMessage(_menu.HuntedLE.Text));
    }

    private void BountyHSelect_OnPressed(ButtonEventArgs obj)
    {
        SendMessage(new JobNetSelectRogueNetMessage(RogueNetworkType.BountyHunter));
    }

    private void AssassinSelect_OnPressed(ButtonEventArgs obj)
    {
        SendMessage(new JobNetSelectRogueNetMessage(RogueNetworkType.Assassin));
    }

    private void DealerSelect_OnPressed(ButtonEventArgs obj)
    {
        SendMessage(new JobNetSelectRogueNetMessage(RogueNetworkType.Dealer));
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
}
