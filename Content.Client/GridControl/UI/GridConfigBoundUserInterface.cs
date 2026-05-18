using Content.Client.Access.UI;
using Content.Client.GridControl.UI;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Doors.Electronics;
using Content.Shared.GridControl.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Content.Shared.GridControl.Components.GridConfigComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.GridControl.UI
{
    public sealed partial class GridConfigBoundUserInterface : BoundUserInterface
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        private readonly SharedGridConfigSystem _gridConfigSystem = default!;

        private GridConfigWindow? _window;

        public GridConfigBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _gridConfigSystem = EntMan.System<SharedGridConfigSystem>();
        }

        protected override void Open()
        {
            base.Open();
            _window = this.CreateWindow<GridConfigWindow>();
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
            _window.BUI = this;
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (GridConfigBoundUserInterfaceState)state;
            _window?.UpdateState(_prototypeManager, castState);
        }

    }
}
