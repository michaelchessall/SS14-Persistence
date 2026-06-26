using System.Linq;
using Content.Shared._Persistence14.RandomTable;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Server._Persistence14.DebugStick;

public sealed partial class DebugStickSystem : EntitySystem
{
    [Dependency] private ILogManager _log = default!;
    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        _sawmill = _log.GetSawmill("debugstick");

        SubscribeLocalEvent<DebugStickComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DebugStickComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    public void OnUseInHand(Entity<DebugStickComponent> stick, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;
        _sawmill.Info($"Debug Stick Activated In Hand");


        Use(stick);
        args.Handled = true;
    }

    public void OnActivateInWorld(Entity<DebugStickComponent> stick, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        _sawmill.Info($"Debug Stick Activated In World");

        Use(stick);
        args.Handled = true;
    }

    private void Use(Entity<DebugStickComponent> stick)
    {
        _sawmill.Info($"Running Random Table");
        var table = stick.Comp.Table;

        var tableSystem = new RandomTableSystem();
        IoCManager.InjectDependencies(tableSystem);

        var run = tableSystem.Run<ReagentQuantity>(table);
        var output = run.ToList();

        _sawmill.Info($"Table Run Produced {output.Count} results");
        int i = 0;
        foreach (var item in output)
        {
            _sawmill.Info($"Item {i}: {item.Reagent} - {item.Quantity}u");
            i++;
        }
    }
}