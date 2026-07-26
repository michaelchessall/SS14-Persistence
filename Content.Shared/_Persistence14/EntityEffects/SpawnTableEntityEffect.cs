using Content.Shared._Persistence14.RandomTable;
using Content.Shared._Persistence14.RandomTable.Selectors;
using Content.Shared.EntityEffects;

namespace Content.Shared._Persistence14.EntityEffects;

public sealed partial class SpawnTable : EntityEffectBase<SpawnTable>
{
    /// <summary>
    /// Random table used to select the spawnable prototype.
    /// </summary>
    [DataField]
    public RandomTableSelector Table = new RandomTableNullSelector();
}