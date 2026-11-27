using Content.Shared._Persistence14.Cord;
using Content.Shared._Persistence14.RandomTable;
using Content.Shared._Persistence14.RandomTable.Selectors;
using Robust.Shared.Prototypes;

namespace Content.Server._Persistence14.DebugStick;

[RegisterComponent, Access(typeof(DebugStickSystem))]
public sealed partial class DebugStickComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<CordComponent> Cord = default!;
}