using Content.Server.Actions;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.Inventory;
using Content.Server.Polymorph.Components;
using Content.Server._Persistence14.EntityVoid;
using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared.Administration;
using Content.Shared.Body;
using Content.Shared.Buckle;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared._Persistence14.EntityVoid;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private PersistentIdentifierSystem _pid = default!;
    [Dependency] private AdminVerbSystem _adminVerb = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private EntityVoidSystem _void = default!;

    private const string RevertPolymorphId = "ActionRevertPolymorph";

    public override void Initialize()
    {
        SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<PolymorphedEntityComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(OnPolymorphActionEvent);
        SubscribeLocalEvent<PolymorphedEntityComponent, RevertPolymorphActionEvent>(OnRevertPolymorphActionEvent);

        SubscribeLocalEvent<PolymorphedEntityComponent, BeforeFullySlicedEvent>(OnBeforeFullySliced);
        SubscribeLocalEvent<PolymorphedEntityComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<PolymorphedEntityComponent, EntityTerminatingEvent>(OnPolymorphedTerminating);

        SubscribeLocalEvent<PolymorphedEntityComponent, GetVerbsEvent<Verb>>(AddPolymorphVerbs);

        InitializeMap();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PolymorphedEntityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Time += frameTime;

            if (comp.Configuration.Duration != null && comp.Time >= comp.Configuration.Duration)
            {
                Revert((uid, comp));
                continue;
            }

            if (!TryComp<MobStateComponent>(uid, out var mob))
                continue;

            if (comp.Configuration.RevertOnDeath && _mobState.IsDead(uid, mob) ||
                comp.Configuration.RevertOnCrit && _mobState.IsIncapacitated(uid, mob))
            {
                Revert((uid, comp));
            }
        }
    }

    private void OnComponentStartup(Entity<PolymorphableComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.InnatePolymorphs != null)
        {
            foreach (var morph in ent.Comp.InnatePolymorphs)
            {
                CreatePolymorphAction(morph, ent);
            }
        }
    }

    private void OnMapInit(Entity<PolymorphedEntityComponent> ent, ref MapInitEvent args)
    {
        var (uid, component) = ent;
        if (component.Configuration.Forced)
            return;

        /*
        if (_actions.AddAction(uid, ref component.Action, out var action, RevertPolymorphId) &&
            _pid.TryResolveId(component.Parent, out var parentEnt))
        {
            _actions.SetEntityIcon((component.Action.Value, action), parentEnt);
            _actions.SetUseDelay(component.Action.Value, TimeSpan.FromSeconds(component.Configuration.Delay));
        }*/
    }

    private void OnPolymorphActionEvent(Entity<PolymorphableComponent> ent, ref PolymorphActionEvent args)
    {
        if (!_proto.Resolve(args.ProtoId, out var prototype) || args.Handled)
            return;

        PolymorphEntity(ent, prototype.Configuration);

        args.Handled = true;
    }

    private void OnRevertPolymorphActionEvent(Entity<PolymorphedEntityComponent> ent,
        ref RevertPolymorphActionEvent args)
    {
        Revert((ent, ent));
    }

    private void OnBeforeFullySliced(Entity<PolymorphedEntityComponent> ent, ref BeforeFullySlicedEvent args)
    {
        if (ent.Comp.Reverted || !ent.Comp.Configuration.RevertOnEat)
            return;

        args.Cancel();
        Revert((ent, ent));
    }

    /// <summary>
    /// It is possible to be polymorphed into an entity that can't "die", but is instead
    /// destroyed. This handler ensures that destruction is treated like death.
    /// </summary>
    private void OnDestruction(Entity<PolymorphedEntityComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Reverted || !ent.Comp.Configuration.RevertOnDeath)
            return;

        Revert((ent, ent));
    }

    private void OnPolymorphedTerminating(Entity<PolymorphedEntityComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Reverted)
            return;

        if (ent.Comp.Configuration.RevertOnDelete)
            Revert(ent.AsNullable());
    }

    /// <summary>
    /// Polymorphs the target entity into the specific polymorph prototype
    /// </summary>
    /// <param name="uid">The entity that will be transformed</param>
    /// <param name="protoId">The id of the polymorph prototype</param>
    public EntityUid? PolymorphEntity(EntityUid uid, ProtoId<PolymorphPrototype> protoId)
    {
        var config = _proto.Index(protoId).Configuration;
        return PolymorphEntity(uid, config);
    }

    /// <summary>
    /// Polymorphs the target entity into another.
    /// </summary>
    /// <param name="uid">The entity that will be transformed</param>
    /// <param name="configuration">The new polymorph configuration</param>
    /// <returns>The new entity, or null if the polymorph failed.</returns>
    public EntityUid? PolymorphEntity(EntityUid uid, PolymorphConfiguration configuration)
    {
        if (HasComp<VoidedComponent>(uid))
            return null;

        // If they're morphed, check their current config to see if they can be
        // morphed again
        if (!configuration.IgnoreAllowRepeatedMorphs
            && TryComp<PolymorphedEntityComponent>(uid, out var currentPoly)
            && !currentPoly.Configuration.AllowRepeatedMorphs)
            return null;

        // If this polymorph has a cooldown, check if that amount of time has passed since the
        // last polymorph ended.
        if (TryComp<PolymorphableComponent>(uid, out var polymorphableComponent) &&
            polymorphableComponent.LastPolymorphEnd != null &&
            _gameTiming.CurTime < polymorphableComponent.LastPolymorphEnd + configuration.Cooldown)
            return null;

        var pid = _pid.EnsureId(uid, out var pidEntity);

        // mostly just for vehicles
        _buckle.TryUnbuckle(uid, uid, true);

        var targetTransformComp = Transform(uid);

        if (configuration.PolymorphSound != null)
            _audio.PlayPvs(configuration.PolymorphSound, targetTransformComp.Coordinates);

        var child = Spawn(configuration.Entity, _transform.GetMapCoordinates(uid, targetTransformComp), rotation: _transform.GetWorldRotation(uid));

        if (configuration.PolymorphPopup != null)
            _popup.PopupEntity(Loc.GetString(configuration.PolymorphPopup,
                ("parent", Identity.Entity(uid, EntityManager)),
                ("child", Identity.Entity(child, EntityManager))),
                child);

        _mindSystem.MakeSentient(child);

        var polymorphedComp = Factory.GetComponent<PolymorphedEntityComponent>();
        polymorphedComp.Configuration = configuration;
        polymorphedComp.ParentPersistentId = pid;
        AddComp(child, polymorphedComp);

        var childXform = Transform(child);
        _transform.SetLocalRotation(child, targetTransformComp.LocalRotation, childXform);

        if (_container.TryGetContainingContainer((uid, targetTransformComp, null), out var cont))
            _container.Insert(child, cont);

        //Transfers all damage from the original to the new one
        if (configuration.TransferDamage &&
            TryComp<DamageableComponent>(child, out var damageParent) &&
            _mobThreshold.GetScaledDamage(uid, child, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage((child, damageParent), damage);
        }

        if (configuration.Inventory == PolymorphInventoryChange.Transfer)
        {
            _inventory.TransferEntityInventories(uid, child);
            foreach (var hand in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, hand, checkActionBlocker: false);
                _hands.TryPickupAnyHand(child, hand);
            }
        }
        else if (configuration.Inventory == PolymorphInventoryChange.Drop)
        {
            if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
            {
                while (enumerator.MoveNext(out var slot))
                {
                    _inventory.TryUnequip(uid, slot.ID, true, true);
                }
            }

            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held);
            }
        }

        if (configuration.TransferName && TryComp(uid, out MetaDataComponent? targetMeta))
            _metaData.SetEntityName(child, targetMeta.EntityName);

        if (configuration.TransferHumanoidAppearance)
        {
            _visualBody.CopyAppearanceFrom(uid, child);
        }

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, child, mind: mind);

        if (!_void.TryVoidEntity(uid))
        {
            if (_mindSystem.TryGetMind(child, out var childMindId, out var childMind))
                _mindSystem.TransferTo(child, childMindId, mind: childMind);
            QueueDel(child);
            return null;
        }

        // Raise an event to inform anything that wants to know about the entity swap
        var ev = new PolymorphedEvent(uid, child, false);
        RaiseLocalEvent(uid, ref ev);

        // visual effect spawn
        if (configuration.EffectProto != null)
            SpawnAttachedTo(configuration.EffectProto, child.ToCoordinates());

        return child;
    }

    /// <summary>
    /// Reverts a polymorphed entity back into its original form
    /// </summary>
    /// <param name="uid">The entityuid of the entity being reverted</param>
    /// <param name="component"></param>
    public EntityUid? Revert(Entity<PolymorphedEntityComponent?> ent)
    {
        var (uid, component) = ent;
        if (!Resolve(ent, ref component))
            return null;

        if (Deleted(uid))
            return null;

        if (ent.Comp?.ParentPersistentId is not { } pid)
            return null;

        var uidXform = Transform(uid);

        // Don't swap back onto a terminating grid
        if (TerminatingOrDeleted(uidXform.ParentUid))
            return null;

        var coords = _transform.ToMapCoordinates(uidXform.Coordinates);

        if (!_void.TryReconstructEntity(pid, coords, out var parentEnt))
            return null;

        if (component.Configuration.ExitPolymorphSound != null)
            _audio.PlayPvs(component.Configuration.ExitPolymorphSound, uidXform.Coordinates);

        component.Reverted = true;

        if (component.Configuration.TransferDamage &&
            TryComp<DamageableComponent>(parentEnt, out var damageParent) &&
            _mobThreshold.GetScaledDamage(uid, parentEnt, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage((parentEnt, damageParent), damage);
        }

        if (component.Configuration.Inventory == PolymorphInventoryChange.Transfer)
        {
            _inventory.TransferEntityInventories(uid, parentEnt);
            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held);
                _hands.TryPickupAnyHand(parentEnt, held, checkActionBlocker: false);
            }
        }
        else if (component.Configuration.Inventory == PolymorphInventoryChange.Drop)
        {
            if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
            {
                while (enumerator.MoveNext(out var slot))
                {
                    _inventory.TryUnequip(uid, slot.ID);
                }
            }

            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held);
            }
        }

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, parentEnt, mind: mind);

        if (TryComp<PolymorphableComponent>(parentEnt, out var polymorphableComponent))
            polymorphableComponent.LastPolymorphEnd = _gameTiming.CurTime;

        var parentXform = Transform(parentEnt);

        // if an item polymorph was picked up, put it back down after reverting
        _transform.AttachToGridOrMap(parentEnt, parentXform);

        // Raise an event to inform anything that wants to know about the entity swap
        var ev = new PolymorphedEvent(uid, parentEnt, true);
        RaiseLocalEvent(uid, ref ev);

        // visual effect spawn
        if (component.Configuration.EffectProto != null)
            SpawnAttachedTo(component.Configuration.EffectProto, parentEnt.ToCoordinates());

        if (component.Configuration.ExitPolymorphPopup != null)
            _popup.PopupEntity(Loc.GetString(component.Configuration.ExitPolymorphPopup,
                ("parent", Identity.Entity(uid, EntityManager)),
                ("child", Identity.Entity(parentEnt, EntityManager))),
                parentEnt);
        QueueDel(uid);

        return parentEnt;
    }

    /// <summary>
    /// Creates a sidebar action for an entity to be able to polymorph at will
    /// </summary>
    /// <param name="id">The string of the id of the polymorph action</param>
    /// <param name="target">The entity that will be gaining the action</param>
    public void CreatePolymorphAction(ProtoId<PolymorphPrototype> id, Entity<PolymorphableComponent> target)
    {
        target.Comp.PolymorphActions ??= new();
        if (target.Comp.PolymorphActions.ContainsKey(id))
            return;

        if (!_proto.Resolve(id, out var polyProto))
            return;

        var entProto = _proto.Index(polyProto.Configuration.Entity);

        EntityUid? actionId = default!;
        if (!_actions.AddAction(target, ref actionId, RevertPolymorphId, target))
            return;

        target.Comp.PolymorphActions.Add(id, actionId.Value);

        var metaDataCache = MetaData(actionId.Value);
        _metaData.SetEntityName(actionId.Value, Loc.GetString("polymorph-self-action-name", ("target", entProto.Name)), metaDataCache);
        _metaData.SetEntityDescription(actionId.Value, Loc.GetString("polymorph-self-action-description", ("target", entProto.Name)), metaDataCache);

        if (_actions.GetAction(actionId) is not { } action)
            return;

        _actions.SetIcon((action, action.Comp), new SpriteSpecifier.EntityPrototype(polyProto.Configuration.Entity));
        _actions.SetEvent(action, new PolymorphActionEvent(id));
    }

    public void RemovePolymorphAction(ProtoId<PolymorphPrototype> id, Entity<PolymorphableComponent> target)
    {
        if (target.Comp.PolymorphActions is not { } actions)
            return;

        if (actions.TryGetValue(id, out var action))
            _actions.RemoveAction(target.Owner, action);
    }

    public void AddPolymorphVerbs(EntityUid uid, PolymorphedEntityComponent component, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        args.Verbs.Add(new Verb
        {
            Text = "revert-polymorph-verb",
            Category = VerbCategory.Admin,
            Act = () => Revert((uid, component)),
        });
    }
}
