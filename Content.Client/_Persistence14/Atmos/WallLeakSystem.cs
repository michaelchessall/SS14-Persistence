using Content.Client.IconSmoothing;
using Content.Shared._Persistence14.Atmos;

namespace Content.Client.WallLeaks;

public sealed partial class WallAtmosLeakSystem : EntitySystem
{
    [Dependency] private readonly IconSmoothSystem _smooth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<WallAtmosLeakEvent>(OnWallAtmosLeakChanged);
    }

    private void OnWallAtmosLeakChanged(WallAtmosLeakEvent args)
    {
        var uid = GetEntity(args.Entity);
        if (!Exists(uid) || !TryComp<IconSmoothComponent>(uid, out var iconSmooth))
            return;

        _smooth.SetEnabled(uid, !args.Leaking, iconSmooth);
    }
}
