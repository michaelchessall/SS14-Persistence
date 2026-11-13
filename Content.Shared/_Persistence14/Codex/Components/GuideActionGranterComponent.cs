
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Persistence14.Codex.Components;

[RegisterComponent]
public sealed partial class GuideActionGranterComponent : Component
{
    public EntProtoId? Action = "ActionPersistenceGuide";
    [DataField]
    public bool Removed = false;
    [DataField]
    public TimeSpan RemoveTime = TimeSpan.FromHours(2);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RemoveIn = TimeSpan.Zero;

    [DataField]
    public EntityUid? ActionEntity;
}
