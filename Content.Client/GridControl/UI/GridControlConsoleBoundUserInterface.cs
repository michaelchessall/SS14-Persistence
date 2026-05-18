using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;
using static Content.Shared.GridControl.Components.GridControlConsoleComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.GridControl.UI
{
    public sealed partial class GridControlConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;

        private GridControlConsoleWindow? _window;

        public GridControlConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();



            _window = new GridControlConsoleWindow(this, _prototypeManager)
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
            var castState = (GridControlConsoleBoundUserInterfaceState)state;
            _window?.UpdateState(castState);
        }



    }
}
