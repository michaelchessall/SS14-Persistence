using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed partial class TraitSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    public void AddTrait(EntityUid mob, TraitPrototype traitPrototype)
    {
        // Add all components required by the prototype
        if (traitPrototype.Components.Count > 0)
            EntityManager.AddComponents(mob, traitPrototype.Components, false);
        // Add all JobSpecials required by the prototype
        foreach (var special in traitPrototype.Specials)
        {
            special.AfterEquip(mob);
        }
        // Add item required by the trait
        if (traitPrototype.TraitGear == null)
            return;
        if (!TryComp(mob, out HandsComponent? handsComponent))
            return;
        var coords = Transform(mob).Coordinates;
        var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
        _sharedHandsSystem.TryPickup(mob,
            inhandEntity,
            checkActionBlocker: false,
            handsComp: handsComponent);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Check if player's job allows to apply traits
        if (args.JobId == null ||
            !ProtoMan.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        foreach (var traitId in args.Profile.TraitPreferences)
        {
            if (!ProtoMan.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Error($"No trait found with ID {traitId}!");
                return;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, args.Mob) ||
                _whitelistSystem.IsWhitelistPass(traitPrototype.Blacklist, args.Mob))
                continue;

            AddTrait(args.Mob, traitPrototype);
        }
    }
}
