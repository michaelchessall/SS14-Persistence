using Content.Shared.Containers.ItemSlots;
using Content.Shared.Invoices.Components;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Invoices.Systems
{
    [UsedImplicitly]
    public abstract class SharedInvoicePrinterConsoleSystem : EntitySystem
    {
        [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private ILogManager _log = default!;

        public const string Sawmill = "idconsole";
        protected ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = _log.GetSawmill(Sawmill);

            SubscribeLocalEvent<InvoicePrinterConsoleComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<InvoicePrinterConsoleComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, InvoicePrinterConsoleComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, InvoicePrinterConsoleComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
        }

        private void OnComponentRemove(EntityUid uid, InvoicePrinterConsoleComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
        }

        [Serializable, NetSerializable]
        private sealed class InvoicePrinterConsoleComponentState : ComponentState
        {

            public InvoicePrinterConsoleComponentState()
            {
            }
        }
    }
}
