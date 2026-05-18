using Content.Server.Solar.Components;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Solar.EntitySystems
{

    [UsedImplicitly]
    internal sealed partial class SolarPositioningSystem : EntitySystem
    {

        [Dependency] private IRobustRandom _robustRandom = default!;
        [Dependency] private SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolarLocationComponent, MapInitEvent>(OnSolarLocationMapInit);
            SubscribeLocalEvent<MapComponent, MapCreatedEvent>(OnMapCreated);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQuery<SolarLocationComponent>();
            foreach (var comp in query)
            {
                comp.TowardsSun += comp.SunAngularVelocity * frameTime;
                comp.TowardsSun = comp.TowardsSun.Reduced();
            }
        }

        private void OnSolarLocationMapInit(Entity<SolarLocationComponent> ent, ref MapInitEvent args)
        {
            RandomizeSun(ent, ent.Comp);
        }

        private void OnMapCreated(Entity<MapComponent> ent, ref MapCreatedEvent args)
        {
            var comp = EnsureComp<SolarLocationComponent>(ent);
            RandomizeSun(ent, comp);
        }

        private void RandomizeSun(EntityUid eid, SolarLocationComponent solarLocation)
        {
            // If the sun has already been randomized
            if (solarLocation.WasInitialized)
                return;

            // Initialize the sun to something random
            solarLocation.TowardsSun = MathHelper.TwoPi * _robustRandom.NextDouble();
            solarLocation.SunAngularVelocity = Angle.FromDegrees(0.1 + ((_robustRandom.NextDouble() - 0.5) * 0.05));
            solarLocation.WasInitialized = true;
        }

        public SolarLocationComponent? GetSolarLocation(EntityUid gridUid)
        {
            var mapUid = _transform.GetMap(gridUid);
            if (mapUid.HasValue && TryComp<SolarLocationComponent>(mapUid, out var comp))
                return comp;
            return null;
        }
    }
}
