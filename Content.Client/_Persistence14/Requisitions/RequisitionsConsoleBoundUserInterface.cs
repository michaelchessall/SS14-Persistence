using Content.Client._Persistence14.Requisitions.UI;
using Content.Shared._Persistence14.Requisitions;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Persistence14.Requisitions;

[UsedImplicitly]
public sealed class RequisitionsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RequisitionsConsoleMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<RequisitionsConsoleMenu>();
        _menu.SetEntity(Owner);

        _menu.OnCheckout += (items, printInvoice, title, overridePrice) => SendMessage(new RequisitionCheckoutMessage(items, printInvoice, title, overridePrice));
        _menu.OnCancel += () => SendMessage(new RequisitionCancelMessage());
        _menu.OnToggleLink += netEnt => SendMessage(new ToggleRequisitionLinkMessage(netEnt));
        _menu.OnSetMaterialPrice += (mat, price) => SendMessage(new RequisitionSetMaterialPriceMessage(mat, price));
        _menu.OnSetFee += fee => SendMessage(new RequisitionSetFeeMessage(fee));
        _menu.OnRemoveFee += id => SendMessage(new RequisitionRemoveFeeMessage(id));
        _menu.OnEjectFlatpacks += () => SendMessage(new RequisitionEjectFlatpacksMessage());
        _menu.OnSetDetailedInvoice += detailed => SendMessage(new RequisitionSetDetailedInvoiceMessage(detailed));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is RequisitionsConsoleState reqState)
            _menu?.Update(reqState);
    }
}
