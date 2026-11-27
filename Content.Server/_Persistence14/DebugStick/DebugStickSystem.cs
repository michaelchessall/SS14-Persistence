using System.Linq;
using Content.Server._Persistence14.Cord;
using Content.Shared._Persistence14.RandomTable;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Server._Persistence14.DebugStick;

public sealed partial class DebugStickSystem : EntitySystem
{
    [Dependency] private CordSystem _cord = default!;

    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        _sawmill = LogManager.GetSawmill("debugstick");
    }

    [SubscribeLocalEvent]
    public void OnUseInHand(Entity<DebugStickComponent> stick, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;
        _sawmill.Info($"Debug Stick Activated In Hand");


        Use(stick, args.User);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    public void OnActivateInWorld(Entity<DebugStickComponent> stick, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        _sawmill.Info($"Debug Stick Activated In World");

        Use(stick, args.User);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<DebugStickComponent> stick, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;
        _sawmill.Info($"Debug Stick used to interact with {ToPrettyString(args.Target)} by {ToPrettyString(args.User)}");

        if (args.Target is not { } target)
        {
            return;
        }

        Interact(stick, target, args.User);
    }

    private void Use(Entity<DebugStickComponent> stick, EntityUid user)
    {

    }

    private void Interact(Entity<DebugStickComponent> stick, EntityUid target, EntityUid user)
    {
        _cord.TrySpawnCord(stick.Owner, target, stick.Comp.Cord, out var cord, true, true);
    }
}