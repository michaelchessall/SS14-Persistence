using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.GridControl.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.GridControl.Systems;

[UsedImplicitly]
public abstract partial class SharedBluespaceParkingSystem : EntitySystem
{
    [Dependency] protected ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] protected ILogManager _log = default!;
    [Dependency] protected SharedAppearanceSystem _appearance = default!;

    public const string Sawmill = "BluespaceParking";
    protected ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill(Sawmill);

        SubscribeLocalEvent<BSPAnchorKeyComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<BSPAnchorKeyComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BSPAnchorKeyComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<BSPAnchorKeyComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
    }


    private void OnComponentInit(EntityUid uid, BSPAnchorKeyComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, BSPAnchorKeyComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
        UpdateAppearance(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, BSPAnchorKeyComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
    }

    private void OnItemSlotChanged(EntityUid uid, BSPAnchorKeyComponent component, ContainerModifiedMessage args)
    {
        UpdateAppearance(uid, component);
    }

    protected void UpdateAppearance(EntityUid uid, BSPAnchorKeyComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var hasId = component.PrivilegedIdSlot.Item != null;
        var hasGrid = !string.IsNullOrEmpty(component.SavedFilename);
        BSPAnchorKeyVisualState state = BSPAnchorKeyVisualState.NoIdUnstored;
        if (hasId && hasGrid)
            state = BSPAnchorKeyVisualState.IdStored;
        else if (hasId)
            state = BSPAnchorKeyVisualState.IdUnstored;
        else if (hasGrid)
            state = BSPAnchorKeyVisualState.NoIdStored;
        _appearance.SetData(uid, BSPAnchorKeyVisuals.State, state, appearance);
    }

    [Serializable, NetSerializable]
    public sealed partial class BSPAnchorKeyDoAfterEvent : DoAfterEvent
    {
        public BSPAnchorKeyDoAfterEvent()
        {
        }

        public override DoAfterEvent Clone() => this;
    }

}
