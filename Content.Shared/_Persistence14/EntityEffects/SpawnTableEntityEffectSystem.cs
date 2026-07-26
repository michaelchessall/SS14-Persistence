using Content.Shared._Persistence14.RandomTable;
using Content.Shared._Persistence14.RandomTable.Selectors;
using Content.Shared.EntityEffects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.EntityEffects;

public sealed partial class SpawnTableEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnTable>
{
    [Dependency] private readonly RandomTableSystem _table = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnTable> args)
    {
        if (!_net.IsServer)
            return;

        var run = _table.RunPrototype<EntityPrototype>(args.Effect.Table);

        foreach (var item in run)
        {
            SpawnNextToOrDrop(item.ID, entity, entity.Comp);
        }
    }
}