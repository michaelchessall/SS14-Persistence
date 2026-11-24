using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Charges;
using Content.Server.Decals;
using Content.Server.Destructible;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Server.SprayPainter;

/// <summary>
/// Handles spraying pipes and decals using a spray painter.
/// Other paintable objects are handled in shared.
/// </summary>
public sealed partial class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private AtmosPipeColorSystem _pipeColor = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChargesSystem _charges = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private HandsSystem _hands = default!;

    const float PipeSpraypaintMaxDist = 3.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayPainterComponent, SprayPainterPipeDoAfterEvent>(OnPipeDoAfter);
        SubscribeLocalEvent<SprayPainterComponent, AfterInteractEvent>(OnFloorAfterInteract);
        SubscribeLocalEvent<AtmosPipeColorComponent, AfterInteractUsingEvent>(OnPipeInteract);
        SubscribeLocalEvent<GasCanisterComponent, EntityPaintedEvent>(OnCanisterPainted);
        SubscribeLocalEvent<PaintableNameComponent, EntityPaintedEvent>(OnNamePainted);
    }

    /// <summary>
    /// Handles drawing decals when a spray painter is used to interact with the floor.
    /// Spray painter must have decal painting enabled and enough charges of paint to paint on the floor.
    /// </summary>
    private void OnFloorAfterInteract(Entity<SprayPainterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target != null)
            return;

        if (ent.Comp.ColorPickerEnabled)
        {
            PickColor(ent, ref args);
            return;
        }

        if (!args.CanReach)
            return;

        // Includes both off and all other don't cares
        if (ent.Comp.DecalMode != DecalPaintMode.Add && ent.Comp.DecalMode != DecalPaintMode.Remove)
            return;

        args.Handled = true;
        if (TryComp(ent, out LimitedChargesComponent? charges) && _charges.GetCurrentCharges((ent, charges)) < ent.Comp.DecalChargeCost)
        {
            _popup.PopupEntity(Loc.GetString("spray-painter-interact-no-charges"), args.User, args.User);
            return;
        }

        var position = args.ClickLocation;
        if (ent.Comp.SnapDecals)
            position = position.SnapToGrid(EntityManager);

        if (ent.Comp.DecalMode == DecalPaintMode.Add)
        {
            // Offset painting for adding decals
            position = position.Offset(new(-0.5f));

            if (!_decals.TryAddDecal(ent.Comp.SelectedDecal, position, out _, ent.Comp.SelectedDecalColor, Angle.FromDegrees(ent.Comp.SelectedDecalAngle), 0, false))
                return;
        }
        else
        {
            var gridUid = _transform.GetGrid(args.ClickLocation);
            if (gridUid is not { } grid)
            {
                _popup.PopupEntity(Loc.GetString("spray-painter-interact-nothing-to-remove"), args.User, args.User);
                return;
            }

            var decals = _decals.GetDecalsInRange(grid, position.Position, validDelegate: IsDecalValid);
            if (decals.Count <= 0)
            {
                _popup.PopupEntity(Loc.GetString("spray-painter-interact-nothing-to-remove"), args.User, args.User);
                return;
            }

            foreach (var decal in decals)
            {
                _decals.RemoveDecal(grid, decal.Index);
            }
        }

        _audio.PlayPvs(ent.Comp.SpraySound, ent);

        _charges.TryUseCharges((ent, charges), ent.Comp.DecalChargeCost);

        AdminLogger.Add(LogType.CrayonDraw, LogImpact.Low, $"{ToPrettyString(args.User):user} painted a {ent.Comp.SelectedDecal}");
    }

    /// <summary>
    /// Returns whether <paramref name="decal"/> is valid to interact with when a spray painter is used to interact with the floor.
    /// </summary>
    private bool IsDecalValid(Decal decal)
    {
        if (!ProtoMan.TryIndex<DecalPrototype>(decal.Id, out var decalProto))
            return false;

        return (decalProto.Tags.Contains("station")
            || decalProto.Tags.Contains("markings"))
            && !decalProto.Tags.Contains("dirty");
    }

    /// <summary>
    /// Event handler when gas canisters are painted.
    /// The canister's color should not change when it's destroyed.
    /// </summary>
    private void OnCanisterPainted(Entity<GasCanisterComponent> ent, ref EntityPaintedEvent args)
    {
        var dummy = Spawn(args.Prototype);

        var destructibleComp = EnsureComp<DestructibleComponent>(dummy);
        CopyComp(dummy, ent, destructibleComp);

        Del(dummy);
    }

    /// <summary>
    /// Event handler to update an object's name and desc metadata when it is painted.
    /// </summary>
    private void OnNamePainted(Entity<PaintableNameComponent> ent, ref EntityPaintedEvent args)
    {
        if (!ProtoMan.TryIndex<EntityPrototype>(args.Prototype, out var proto))
            return;

        _metaData.SetEntityName(ent, proto.Name);
        _metaData.SetEntityDescription(ent, proto.Description);
    }

    private void OnPipeDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterPipeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not { } target)
            return;

        if (!TryComp<AtmosPipeColorComponent>(target, out var color))
            return;

        if (TryComp<LimitedChargesComponent>(ent, out var charges) &&
            !_charges.TryUseCharges((ent, charges), ent.Comp.PipeChargeCost))
            return;

        Audio.PlayPvs(ent.Comp.SpraySound, ent);
        _pipeColor.SetColor(target, color, args.Color);

        args.Handled = true;
    }

    private void OnPipeInteract(Entity<AtmosPipeColorComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryPaintPipe(ent, args.Used, args.User);
    }

    private bool TryPaintPipe(Entity<AtmosPipeColorComponent> ent, EntityUid painter, EntityUid user)
    {
        if (!TryComp<SprayPainterComponent>(painter, out var painterComp) ||
            painterComp.PickedColor is not { } colorName ||
            !painterComp.ColorPalette.TryGetValue(colorName, out var color))
            return false;

        if (TryComp<LimitedChargesComponent>(painter, out var chargesComp) &&
            _charges.GetCurrentCharges((painter, chargesComp)) < painterComp.PipeChargeCost)
        {
            var msg = Loc.GetString("spray-painter-interact-no-charges");
            _popup.PopupEntity(msg, user, user);
            return false;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            user,
            painterComp.PipeSprayTime,
            new SprayPainterPipeDoAfterEvent(color),
            painter,
            target: ent,
            used: painter)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            // multiple pipes can be sprayed at once just not the same one
            DuplicateCondition = DuplicateConditions.SameTarget,
            NeedHand = true,
            RequireCanInteract = false,
            DistanceThreshold = null,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        return DoAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void PickColor(Entity<SprayPainterComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.ClickLocation.IsValid(EntityManager) || _transform.GetGrid(args.ClickLocation) is not { } grid)
            return;

        var clickPos = args.ClickLocation.Position;
        var decals = _decals.GetDecalsInRange(grid, clickPos, validDelegate: IsDecalValid);
        if (decals.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("spray-painter-interact-no-color-pick"), args.User, args.User);
            return;
        }

        var closestDecal = decals.MinBy(d => Vector2.Distance(d.Decal.Coordinates, clickPos)).Decal;

        _popup.PopupEntity(Loc.GetString("spray-painter-interact-color-picked", ("id", closestDecal.Id)), args.User, args.User);

        ent.Comp.SelectedDecalColor = closestDecal.Color;
        ent.Comp.ColorPickerEnabled = false;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetAltVerbs(EntityUid uid, AtmosPipeColorComponent pipe, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var active = _hands.GetActiveItem(args.User);
        if (active is not { } item || !TryComp<SprayPainterComponent>(item, out var painterComp))
            return;

        if (!Transform(args.User).Coordinates.TryDistance(EntityManager, Transform(args.Target).Coordinates, out var distance) ||
            distance > PipeSpraypaintMaxDist)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/palette.svg.png")),
            Text = Loc.GetString("paint-verb-text"),

            Act = () => TryPaintPipe((uid, pipe), item, user)
        });
    }

    [SubscribeLocalEvent]
    private void OnPipePaintAttempt(Entity<SprayPainterComponent> ente, ref DoAfterAttemptEvent<SprayPainterPipeDoAfterEvent> args)
    {
        var user = args.DoAfter.Args.User;
        var target = args.DoAfter.Args.Target;

        if (target == null ||
            !Transform(user).Coordinates.TryDistance(EntityManager, Transform(target.Value).Coordinates, out var distance) ||
            distance > PipeSpraypaintMaxDist)
            args.Cancel();
    }
}
