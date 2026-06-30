using Content.Shared._Persistence14.RandomTable;
using Content.Shared._Persistence14.RandomTable.Selectors;

namespace Content.Server._Persistence14.DebugStick;

[RegisterComponent, Access(typeof(DebugStickSystem))]
public sealed partial class DebugStickComponent : Component
{
    [DataField]
    public RandomTableSelector Table = new RandomTableNullSelector();
}