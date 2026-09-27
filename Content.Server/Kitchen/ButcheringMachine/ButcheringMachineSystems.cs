using Content.Server.Botany.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Climbing.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Materials;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Throwing;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.Kitchen.ButcheringMachine
{
    public sealed class ButcheringMachineSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
        [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddleSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly MaterialStorageSystem _material = default!;
        [Dependency] private readonly SharedMindSystem _minds = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;

        public static readonly ProtoId<MaterialPrototype> BiomassPrototype = "Biomass";

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveButcheringMachineComponent, ButcheringMachineComponent>();
            while (query.MoveNext(out var uid, out var _, out var reclaimer))
            {
                reclaimer.ProcessingTimer -= frameTime;
                reclaimer.RandomMessTimer -= frameTime;

                if (reclaimer.RandomMessTimer <= 0)
                {
                    if (_robustRandom.Prob(0.2f) && reclaimer.BloodReagents is { } blood)
                    {
                        _puddleSystem.TrySpillAt(uid, blood, out _);
                    }
                    if (_robustRandom.Prob(0.03f) && reclaimer.SpawnedEntities.Count > 0)
                    {
                        var thrown = Spawn(_robustRandom.Pick(reclaimer.SpawnedEntities).PrototypeId, Transform(uid).Coordinates);
                        var direction = new Vector2(_robustRandom.Next(-30, 30), _robustRandom.Next(-30, 30));
                        _throwing.TryThrow(thrown, direction, _robustRandom.Next(1, 10));
                    }
                    reclaimer.RandomMessTimer += (float)reclaimer.RandomMessInterval.TotalSeconds;
                }

                if (reclaimer.ProcessingTimer > 0)
                {
                    continue;
                }

                foreach (var item in reclaimer.SpawnedEntities)
                {
                    var number = item.Amount;
                    for (int i = 0; i < item.Amount + 1; i++)
                    {
                        number--;
                        var thrown = Spawn(item.PrototypeId, Transform(uid).Coordinates);
                        _transform.DropNextTo(thrown, uid);
                    }
                }

                reclaimer.BloodReagents = null;
                reclaimer.SpawnedEntities.Clear();
                RemCompDeferred<ActiveButcheringMachineComponent>(uid);
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActiveButcheringMachineComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ActiveButcheringMachineComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<ActiveButcheringMachineComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<ButcheringMachineComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<ButcheringMachineComponent, ClimbedOnEvent>(OnClimbedOn);
            SubscribeLocalEvent<ButcheringMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<ButcheringMachineComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
            SubscribeLocalEvent<ButcheringMachineComponent, ReclaimerDoAfterEvent>(OnDoAfter);
        }

        private void OnSuicideByEnvironment(Entity<ButcheringMachineComponent> ent, ref SuicideByEnvironmentEvent args)
        {
            if (args.Handled)
                return;

            if (HasComp<ActiveButcheringMachineComponent>(ent))
                return;

            if (TryComp<ApcPowerReceiverComponent>(ent, out var power) && !power.Powered)
                return;

            _popup.PopupEntity(Loc.GetString("Butchering-Machine-suicide-others", ("victim", args.Victim)), ent, PopupType.LargeCaution);
            StartProcessing(args.Victim, ent);
            args.Handled = true;
        }

        private void OnInit(EntityUid uid, ActiveButcheringMachineComponent component, ComponentInit args)
        {
            _jitteringSystem.AddJitter(uid, -10, 100);
            _sharedAudioSystem.PlayPvs("/Audio/Machines/reclaimer_startup.ogg", uid);
            _ambientSoundSystem.SetAmbience(uid, true);
        }

        private void OnShutdown(EntityUid uid, ActiveButcheringMachineComponent component, ComponentShutdown args)
        {
            RemComp<JitteringComponent>(uid);
            _ambientSoundSystem.SetAmbience(uid, false);
        }

        private void OnPowerChanged(EntityUid uid, ButcheringMachineComponent component, ref PowerChangedEvent args)
        {
            if (args.Powered)
            {
                if (component.ProcessingTimer > 0)
                    EnsureComp<ActiveButcheringMachineComponent>(uid);
            }
            else
                RemComp<ActiveButcheringMachineComponent>(uid);
        }

        private void OnUnanchorAttempt(EntityUid uid, ActiveButcheringMachineComponent component, UnanchorAttemptEvent args)
        {
            args.Cancel();
        }
        private void OnAfterInteractUsing(Entity<ButcheringMachineComponent> reclaimer, ref AfterInteractUsingEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (!CanGib(reclaimer, args.Used))
                return;

            if (!TryComp<PhysicsComponent>(args.Used, out var physics))
                return;

            var delay = reclaimer.Comp.BaseInsertionDelay * physics.FixturesMass;
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, delay, new ReclaimerDoAfterEvent(), reclaimer, target: args.Target, used: args.Used)
            {
                NeedHand = true,
                BreakOnMove = true,
            });
        }

        private void OnClimbedOn(Entity<ButcheringMachineComponent> reclaimer, ref ClimbedOnEvent args)
        {
            if (!CanGib(reclaimer, args.Climber))
            {
                var direction = new Vector2(_robustRandom.Next(-2, 2), _robustRandom.Next(-2, 2));
                _throwing.TryThrow(args.Climber, direction, 0.5f);
                return;
            }
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Instigator):player} used a biomass reclaimer to gib {ToPrettyString(args.Climber):target} in {ToPrettyString(reclaimer):reclaimer}");

            StartProcessing(args.Climber, reclaimer);
        }

        private void OnDoAfter(Entity<ButcheringMachineComponent> reclaimer, ref ReclaimerDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (args.Args.Used == null || args.Args.Target == null || !HasComp<ButcheringMachineComponent>(args.Args.Target.Value))
                return;

            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Args.User):player} used a biomass reclaimer to gib {ToPrettyString(args.Args.Target.Value):target} in {ToPrettyString(reclaimer):reclaimer}");
            StartProcessing(args.Args.Used.Value, reclaimer);

            args.Handled = true;
        }

        private void StartProcessing(EntityUid toProcess, Entity<ButcheringMachineComponent> ent, PhysicsComponent? physics = null)
        {
            if (!Resolve(toProcess, ref physics))
                return;

            var component = ent.Comp;
            AddComp<ActiveButcheringMachineComponent>(ent);

            if (TryComp<BloodstreamComponent>(toProcess, out var stream) &&
                _solution.ResolveSolution(toProcess, stream.BloodSolutionName, ref stream.BloodSolution, out var solution))
            {
                component.BloodReagents = solution.Clone();
                component.BloodReagents.ScaleSolution(50 / component.BloodReagents.Volume);
            }
            if (TryComp<ButcherableComponent>(toProcess, out var butcherableComponent))
            {
                component.SpawnedEntities = butcherableComponent.SpawnedEntities;
            }

            component.ProcessingTimer = physics.FixturesMass * component.ProcessingTimePerUnitMass;

            var inventory = _inventory.GetHandOrInventoryEntities(toProcess);
            foreach (var item in inventory)
            {
                _transform.DropNextTo(item, ent.Owner);
            }

            QueueDel(toProcess);
        }

        private bool CanGib(Entity<ButcheringMachineComponent> reclaimer, EntityUid dragged)
        {
            if (HasComp<ActiveButcheringMachineComponent>(reclaimer))
                return false;

            bool isPlant = HasComp<ProduceComponent>(dragged);
            if (!isPlant && !HasComp<MobStateComponent>(dragged))
                return false;

            if (!Transform(reclaimer).Anchored)
                return false;

            if (TryComp<ApcPowerReceiverComponent>(reclaimer, out var power) && !power.Powered)
                return false;

            if (!isPlant && reclaimer.Comp.SafetyEnabled && !_mobState.IsDead(dragged))
                return false;

            // Reject souled bodies in easy mode.
            if (_configManager.GetCVar(CCVars.BiomassEasyMode) &&
                HasComp<HumanoidProfileComponent>(dragged) &&
                _minds.TryGetMind(dragged, out _, out var mind))
            {
                if (mind.UserId != null && _playerManager.TryGetSessionById(mind.UserId.Value, out _))
                    return false;
            }

            return true;
        }
    }
}
