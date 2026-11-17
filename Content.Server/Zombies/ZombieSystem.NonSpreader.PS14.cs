using Content.Shared.SSDIndicator;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.Zombies;

public sealed partial class ZombieSystem
{
    [Dependency] private StatusEffectsSystem _effectsSystem = default!;

    [Dependency] private EntityQuery<SSDIndicatorComponent> _indicatorQuery;

    private readonly EntProtoId SsdForceSleepProto = "StatusEffectSSDSleeping";

    [SubscribeLocalEvent]
    private void Startup(Entity<NonSpreaderZombieComponent> ent, ref ComponentStartup args)
    {
        if (_effectsSystem.HasStatusEffect(ent.Owner, SsdForceSleepProto))
            _effectsSystem.TryRemoveStatusEffect(ent.Owner, SsdForceSleepProto);

        if (_indicatorQuery.HasComponent(ent.Owner))
            RemCompDeferred<SSDIndicatorComponent>(ent.Owner);
    }
}
