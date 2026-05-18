using Content.Client.Access.UI;
using Content.Client.GridControl.UI;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Doors.Electronics;
using Content.Shared.GridControl.Components;
using Content.Shared.GridControl.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Content.Shared.GridControl.Components.GridConfigComponent;
using static Content.Shared.GridControl.Components.StationCreatorComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.GridControl.UISC
{
    public sealed partial class StationCreatorBoundUserInterface : BoundUserInterface
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        private readonly SharedGridConfigSystem _gridConfigSystem = default!;

        private StationCreatorWindow? _window;

        public StationCreatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _gridConfigSystem = EntMan.System<SharedGridConfigSystem>();
        }

        protected override void Open()
        {
            base.Open();
            _window = this.CreateWindow<StationCreatorWindow>();
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(StationCreatorComponent.PrivilegedIdCardSlotId));
            _window.BUI = this;
            _window.Title = "station genesis device";
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (StationCreatorBoundUserInterfaceState)state;
            _window?.UpdateState(_prototypeManager, castState);
        }

    }
}
