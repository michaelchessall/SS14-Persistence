using System.Numerics;
using Content.Shared._Persistence14.Storage.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Persistence14.Storage.EntitySystems;

public sealed partial class FalseBottomSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public const string ContainerName = "false_bottom_storage";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FalseBottomComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FalseBottomComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FalseBottomComponent, FalseBottomPryDoAfterEvent>(OnPryDoAfter);
        SubscribeLocalEvent<FalseBottomComponent, StorageCloseAttemptEvent>(OnStorageCloseAttempt);
        SubscribeLocalEvent<FalseBottomComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<FalseBottomComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<FalseBottomComponent, BeforeExplodeEvent>(OnBeforeExplode);
        SubscribeLocalEvent<FalseBottomComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnInit(Entity<FalseBottomComponent> ent, ref ComponentInit args)
    {
        ent.Comp.HiddenContents = _container.EnsureContainer<Container>(ent, ContainerName);
    }

    private void OnInteractUsing(Entity<FalseBottomComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var storage) || !storage.Open)
            return;

        if (_tool.UseTool(args.Used, args.User, ent, ent.Comp.PryTime, ent.Comp.ToolQuality, new FalseBottomPryDoAfterEvent()))
            args.Handled = true;
    }

    private void OnPryDoAfter(Entity<FalseBottomComponent> ent, ref FalseBottomPryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var storage) || !storage.Open)
            return;

        if (!ent.Comp.Open)
        {
            OpenFalseBottom(ent, args.User);
        }
        else
        {
            CloseFalseBottom(ent, storage, args.User);
        }

        args.Handled = true;
    }

    private void OpenFalseBottom(Entity<FalseBottomComponent> ent, EntityUid user)
    {
        ent.Comp.Open = true;
        Dirty(ent);

        _container.EmptyContainer(ent.Comp.HiddenContents, reparent: true);
        _popup.PopupEntity(Loc.GetString("false-bottom-opened-popup"), ent, user);
    }

    private void CloseFalseBottom(Entity<FalseBottomComponent> ent, EntityStorageComponent storage, EntityUid user)
    {
        var entities = _lookup.GetEntitiesInRange(
            new EntityCoordinates(ent, storage.EnteringOffset),
            storage.EnteringRange,
            LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries);

        entities.Remove(ent);

        foreach (var item in entities)
        {
            if (ent.Comp.HiddenContents.ContainedEntities.Count >= ent.Comp.Capacity)
                break;

            if (!CanInsert(item, ent))
                continue;

            _joints.RecursiveClearJoints(item);
            _container.Insert(item, ent.Comp.HiddenContents);
        }

        ent.Comp.Open = false;
        Dirty(ent);

        var count = ent.Comp.HiddenContents.ContainedEntities.Count;
        var msg = count > 0
            ? Loc.GetString("false-bottom-closed-count-popup", ("count", count))
            : Loc.GetString("false-bottom-closed-empty-popup");
        _popup.PopupEntity(msg, ent, user);
    }

    private bool CanInsert(EntityUid item, Entity<FalseBottomComponent> ent)
    {
        if (_container.IsEntityInContainer(item))
            return false;

        if (Transform(item).Anchored)
            return false;

        var aabb = _lookup.GetAABBNoContainer(item, Vector2.Zero, 0);
        if (ent.Comp.MaxSize < aabb.Size.X || ent.Comp.MaxSize < aabb.Size.Y)
            return false;

        return _whitelist.CheckBoth(item, ent.Comp.Blacklist, ent.Comp.Whitelist);
    }

    private void OnStorageCloseAttempt(Entity<FalseBottomComponent> ent, ref StorageCloseAttemptEvent args)
    {
        if (!ent.Comp.Open)
            return;

        args.Cancelled = true;
        if (args.User != null)
            _popup.PopupEntity(Loc.GetString("false-bottom-close-crate-blocked"), ent, args.User.Value);
    }

    private void OnExamined(Entity<FalseBottomComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !ent.Comp.Open)
            return;

        args.PushMarkup(Loc.GetString("false-bottom-examine-open"));
    }

    private void OnDestruction(Entity<FalseBottomComponent> ent, ref DestructionEventArgs args)
    {
        ent.Comp.Open = true;
        Dirty(ent);
        _container.EmptyContainer(ent.Comp.HiddenContents, reparent: true);
    }

    private void OnBeforeExplode(Entity<FalseBottomComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(ent.Comp.HiddenContents.ContainedEntities);
    }

    private void OnGetVerbs(Entity<FalseBottomComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var storage) || !storage.Open)
            return;

        if (args.Using == null || !_tool.HasQuality(args.Using.Value, ent.Comp.ToolQuality))
            return;

        var user = args.User;
        var tool = args.Using.Value;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString(ent.Comp.Open ? "false-bottom-verb-close" : "false-bottom-verb-open"),
            Icon = new SpriteSpecifier.Texture(new(ent.Comp.Open
                ? "/Textures/Interface/VerbIcons/close.svg.192dpi.png"
                : "/Textures/Interface/VerbIcons/open.svg.192dpi.png")),
            Act = () => _tool.UseTool(tool, user, ent, ent.Comp.PryTime, ent.Comp.ToolQuality, new FalseBottomPryDoAfterEvent()),
        });
    }
}

[Serializable, NetSerializable]
public sealed partial class FalseBottomPryDoAfterEvent : SimpleDoAfterEvent;