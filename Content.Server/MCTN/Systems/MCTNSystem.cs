using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.MCTN.Systems;

public sealed partial class MCTNSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPhysicsSystem _physx = default!;


    public override void Initialize()
    {
        base.Initialize();

        InitializePlugs();
        InitializeConnections();
        InitializeTethers();
        InitializeUI();
    }
}
