using Content.Client.GridControl.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.GridControl.Systems;
using Content.Shared.Radio.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Radio.Ui
{
    public sealed partial class HeadsetMenuBoundUserInterface : BoundUserInterface
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;

        private HeadsetMenuWindow? _window;

        public HeadsetMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = this.CreateWindow<HeadsetMenuWindow>();
            _window.BUI = this;
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (HeadsetMenuBoundUserInterfaceState)state;
            _window?.UpdateState(_prototypeManager, castState);
        }

    }
}
