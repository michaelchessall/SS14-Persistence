using Content.Shared._Persistence14.RandomTable.Selectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.RandomTable;

[Prototype]
public sealed partial class RandomTablePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField]
    public RandomTableSelector Table = new RandomTableNullSelector();
}