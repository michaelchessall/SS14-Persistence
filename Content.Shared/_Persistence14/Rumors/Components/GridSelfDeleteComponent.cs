using Content.Shared._Persistence14.Rumors.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Rumors.Components;

[RegisterComponent]
public sealed partial class GridSelfDeleteComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DeleteTime = TimeSpan.Zero;
    [DataField]
    public TimeSpan AnnounceTime = TimeSpan.Zero;
    [DataField]
    public TimeSpan AnnounceCooldown = TimeSpan.FromMinutes(10);
    [DataField]
    public List<EntityUid> SpawnedMobs = new();

}
