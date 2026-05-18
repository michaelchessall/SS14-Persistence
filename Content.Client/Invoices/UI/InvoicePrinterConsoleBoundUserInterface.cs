using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Content.Shared.Invoices.Components;
using Content.Shared.Invoices.Systems;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Invoices.UI
{
    public sealed partial class InvoicePrinterConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        private InvoicePrinterConsoleWindow? _window;


        public InvoicePrinterConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();



            _window = new InvoicePrinterConsoleWindow(this, _prototypeManager)
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName
            };


            _window.OnClose += Close;
            _window.OpenCentered();
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(InvoicePrinterConsoleComponent.PrivilegedIdCardSlotId));
            _window.PossibleStations.OnItemSelected += OnStationSelected;

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();

        }

        private void OnStationSelected(OptionButton.ItemSelectedEventArgs args)
        {
            SendMessage(new InvoicePrinterStationSelectMessage(args.Id));

        }
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (InvoicePrinterConsoleBoundUserInterfaceState)state;
            _window?.UpdateState(castState);
        }


    }
}
