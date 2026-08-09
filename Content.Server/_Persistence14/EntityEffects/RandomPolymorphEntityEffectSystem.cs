using System.Linq;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Persistence14.EntityEffects;
using Content.Shared._Persistence14.RandomTable;
using Content.Shared.EntityEffects;
using Content.Shared.Polymorph;

namespace Content.Server._Persistence14.EntityEffects;

public sealed partial class RandomPolymorphEntityEffectSystem : EntityEffectSystem<PolymorphableComponent, RandomPolymorph>
{
    [Dependency] private readonly RandomTableSystem _randomTable = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public const string Sawmill = "random-polymorph-entity-effect";

    protected override void Effect(Entity<PolymorphableComponent> entity, ref EntityEffectEvent<RandomPolymorph> args)
    {
        var results = _randomTable.RunPrototype<PolymorphPrototype>(args.Effect.Table);

        var count = results.Count();
        if (count <= 0 || (count > 1 && args.Effect.ThrowIfMultiple))
        {
            LogManager.GetSawmill(Sawmill).Error($"Unable to polymorph. Invalid result quantity: {count}");
            return;
        }

        var polymorph = results.First();
        _polymorph.QueuePolymorph(entity.Owner, polymorph);
    }
}