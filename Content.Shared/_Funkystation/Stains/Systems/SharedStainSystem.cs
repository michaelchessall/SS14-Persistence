using Content.Shared._Funkystation.Fluids;
using Content.Shared._Funkystation.Stains.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Funkystation.Stains.Systems;

[Serializable, NetSerializable]
public enum StainVisuals : byte
{
    Toggle
}

public abstract class SharedStainSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = null!;
    [Dependency] private readonly SharedHandsSystem _hands = null!;
    [Dependency] private readonly SharedItemSystem _item = null!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = null!;
    [Dependency] private readonly SharedContainerSystem _container = null!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = null!;
    [Dependency] private readonly InventorySystem _inventory = null!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = null!;
    [Dependency] private readonly SharedPuddleSystem _puddle = null!;
    [Dependency] private readonly SharedPopupSystem _popup = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StainableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StainableComponent, InventoryRelayedEvent<SpilledOnEvent>>(OnSpilledOn);
        SubscribeLocalEvent<StainableComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<StainableComponent, WringStainDoAfterEvent>(OnWring);
        SubscribeLocalEvent<StainableComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(Entity<StainableComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId == ent.Comp.SolutionName)
            UpdateVisuals(ent);
    }

    private void OnMapInit(Entity<StainableComponent> ent, ref MapInitEvent args)
    {
        if (_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var sol))
            sol.Value.Comp.Solution.CanReact = false;
    }

    private void OnSpilledOn(Entity<StainableComponent> ent, ref InventoryRelayedEvent<SpilledOnEvent> args)
    {
        if (IsStainBlocked(ent))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var stainSolution))
            return;

        var transferAmount = FixedPoint.FixedPoint2.Min(args.Args.Solution.Volume, ent.Comp.SpillTransferAmount);
        var split = args.Args.Solution.SplitSolution(transferAmount);

        for (var i = split.Contents.Count - 1; i >= 0; i--)
        {
            if (split.Contents[i].Reagent.Prototype == "Water")
                split.RemoveReagent(split.Contents[i].Reagent, split.Contents[i].Quantity);
        }

        if (split.Volume > 0)
        {
            _solution.TryAddSolution(stainSolution.Value, split);
            UpdateVisuals(ent);
            OnStained(ent, stainSolution.Value);
        }
    }

    protected virtual void OnStained(Entity<StainableComponent> ent, Entity<SolutionComponent> solution) { }

    private bool IsStainBlocked(Entity<StainableComponent> ent)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container) || !TryComp<InventoryComponent>(container.Owner, out var inv))
            return false;

        if (!_inventory.TryGetSlot(container.Owner, container.ID, out var slotDef, inv))
            return false;

        foreach (var slot in inv.Slots)
        {
            if (!_inventory.TryGetSlotEntity(container.Owner, slot.Name, out var slotEnt, inv))
                continue;

            if (TryComp<StainBlockerComponent>(slotEnt, out var blocker) && (blocker.BlockedSlots & slotDef.SlotFlags) != 0)
                return true;
        }

        return false;
    }

    public void UpdateVisuals(Entity<StainableComponent> ent)
    {
        _item.VisualsChanged(ent.Owner);

        if (TryComp<AppearanceComponent>(ent.Owner, out var app))
        {
            var toggled = true;
            if (_appearance.TryGetData(ent.Owner, StainVisuals.Toggle, out bool current, app))
                toggled = !current;

            _appearance.SetData(ent.Owner, StainVisuals.Toggle, toggled, app);
        }
        if (_container.TryGetContainingContainer(ent.Owner, out var container))
        {
            if (TryComp<AppearanceComponent>(container.Owner, out var wearerApp))
            {
                _appearance.QueueUpdate(container.Owner, wearerApp);

                Dirty(container.Owner, wearerApp);
            }
        }
    }

    private void OnGetVerbs(Entity<StainableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Using != ent.Owner)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var sol) || sol.Volume <= 0)
            return;

        MakeWringVerb(args.Target, ent.Comp.WringDoAfterDuration, ref args, "stain-verb-wring");
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ent">The entity to be wrung out</param>
    /// <param name="duration">How long the doafter will take</param>
    /// <param name="args"></param>
    /// <param name="used">The entity the wring verb was used on</param>
    public void MakeWringVerb(EntityUid ent, float duration, ref GetVerbsEvent<Verb> args, string? LocString = null, EntityUid? used = null)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(LocString ?? "stain-verb-wring"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                    user,
                    duration,
                    new WringStainDoAfterEvent(),
                    ent,
                    ent,
                    used)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = true
                });
            }
        });
    }

    private void OnWring(Entity<StainableComponent> ent, ref WringStainDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        TryWring(ent, ref args);
    }

    private void TryWring(Entity<StainableComponent> ent, ref WringStainDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        args.Handled = true;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var solComp, out var sol))
            return;

        var split = _solution.SplitSolution(solComp.Value, sol.Volume);

        if (args.Used is not null && // Wring into drain
            TryComp<DrainComponent>(args.Used, out var drainComp) &&
            drainComp.Solution is not null &&
            _solution.TryAddSolution(drainComp.Solution.Value, split))
        {
            _popup.PopupEntity(Loc.GetString("stain-verb-wring-drain-success"), args.User, args.User);
        }
        else if (_puddle.TrySpillAt(ent.Owner, split, out _)) // Wring onto floor
        {
            _popup.PopupEntity(Loc.GetString("stain-verb-wring-success"), args.User, args.User);
        }
        else // Return to stainable item
        {
            _popup.PopupEntity(Loc.GetString("stain-verb-wring-failure"), args.User, args.User);
            _solution.TryAddSolution(solComp.Value, split);
        }
        UpdateVisuals(ent);
    }
}
