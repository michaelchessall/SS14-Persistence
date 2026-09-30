using Content.Client.Cargo.UI;
using Content.Shared.Cargo.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Cargo.BUI;

[UsedImplicitly]
public sealed class CargoBountyConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CargoBountyMenu? _menu;

    public CargoBountyConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CargoBountyMenu>();

        _menu.OnLabelButtonPressed += id =>
        {
            SendMessage(new BountyPrintLabelMessage(id));
        };

        _menu.OnRumorLabelButtonPressed += id =>
        {
            SendMessage(new BountyPrintRumorLabelMessage(id));
        };

        _menu.OnSkipButtonPressed += id =>
        {
            SendMessage(new BountySkipMessage(id));
        };

        _menu.PossibleTrades.OnItemSelected += OnPossibleTradeSelected;
    }

    private void OnPossibleTradeSelected(OptionButton.ItemSelectedEventArgs args)
    {
        SendMessage(new CargoConsoleSelectTradeMessage(args.Id));
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is not CargoBountyConsoleState state)
            return;

        _menu?.UpdateEntries(state.Bounties, state.History, state.UntilNextSkip, state);
    }
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (_menu == null) return;
        if (message is not BountyRumorBountiesMessage rumormsg) return;
        _menu.UpdateRumor(rumormsg);
    }
}
