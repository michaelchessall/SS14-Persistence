using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Events;
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
using static Robust.Client.UserInterface.Controls.MenuBar;

namespace Content.Client.Invoices.UI
{
    public sealed partial class InvoiceBoundUserInterface : BoundUserInterface
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;

        private InvoiceWindow? _window;

        public InvoiceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();



            _window = new InvoiceWindow(this, _prototypeManager)
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName
            };


            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (InvoiceBoundUserInterfaceState)state;
            _window?.UpdateState(castState);
        }

    }
}
