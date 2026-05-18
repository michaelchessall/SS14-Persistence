using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Weapons.Ranged.Systems;

/// <summary>
///     System for lighting smokeable projectiles shot from weapons which light them
///     (Tactical cigarette dispensers)
/// </summary>
public sealed class LightSmokableOnShootSystem : EntitySystem
{
    [Dependency] private SmokingSystem _smoking = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightSmokableOnShootComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private void OnAmmoShot(Entity<LightSmokableOnShootComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp(projectile, out SmokableComponent? smokable))
                continue;

            if (smokable.State != SmokableState.Unlit)
                continue;

            _smoking.SetSmokableState(projectile, SmokableState.Lit, smokable);
        }
    }
}
