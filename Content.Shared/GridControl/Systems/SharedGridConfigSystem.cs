using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.GridControl.Components;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.GridControl.Systems;

[UsedImplicitly]
public abstract partial class SharedGridConfigSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private ILogManager _log = default!;

    public const string Sawmill = "GridConfig";
    protected ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill(Sawmill);

        SubscribeLocalEvent<GridConfigComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<GridConfigComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StationCreatorComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<StationCreatorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StationTaggerComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<StationTaggerComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, StationCreatorComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, StationCreatorComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
    }

    private void OnComponentRemove(EntityUid uid, StationCreatorComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
    }
    private void OnComponentInit(EntityUid uid, StationTaggerComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, StationTaggerComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
    }

    private void OnComponentRemove(EntityUid uid, StationTaggerComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
    }
    private void OnComponentInit(EntityUid uid, GridConfigComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, GridConfigComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
    }

    private void OnComponentRemove(EntityUid uid, GridConfigComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
    }

    [Serializable, NetSerializable]
    public sealed partial class GridConfigDoAfterEvent : DoAfterEvent
    {
        public GridConfigDoAfterEvent()
        {
        }

        public override DoAfterEvent Clone() => this;
    }

    [Serializable, NetSerializable]
    public sealed partial class StationTaggerDoAfterEvent : DoAfterEvent
    {
        public StationTaggerDoAfterEvent()
        {
        }

        public override DoAfterEvent Clone() => this;
    }
}


[ByRefEvent]
public record struct OnGridConfigAccessUpdatedEvent(EntityUid UserUid, bool Handled = false);
