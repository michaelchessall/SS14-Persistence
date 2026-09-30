using Content.Shared._Persistence14.RandomTable;

namespace Content.Shared._Persistence14.RandomTable.Selectors;

public interface IRandomTableCollectionSelector
{
    public IEnumerable<RandomTableSelector> GetChildren(RandomTableContext ctx);
}