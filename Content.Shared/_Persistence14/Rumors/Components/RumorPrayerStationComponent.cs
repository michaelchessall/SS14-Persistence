using Content.Shared._Persistence14.Rumors.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Rumors.Components;

[RegisterComponent]
public sealed partial class RumorPrayerStationComponent : Component
{
    [DataField]
    public string Tag = "";
}
