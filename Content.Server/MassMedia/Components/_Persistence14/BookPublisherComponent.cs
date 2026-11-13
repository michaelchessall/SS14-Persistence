using Content.Server.MassMedia.Systems._Persistence14;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.MassMedia.Components._Persistence14;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(BookPublisherSystem))]
public sealed partial class BookPublisherComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool PublishEnabled;

    [ViewVariables(VVAccess.ReadWrite), DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPublish;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float PublishCooldown = 60f;

    [DataField]
    public SoundSpecifier NoAccessSound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");

    [DataField]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField(required: true)]
    public ItemSlot BookSlot = new();
}
