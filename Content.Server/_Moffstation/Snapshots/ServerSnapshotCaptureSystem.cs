using System.IO;
using System.Numerics;
using Content.Server._Persistence14.Snapshots;
using Content.Server.MapText;
using Content.Server.Mind;
using Content.Server.Movement.Components;
using Content.Shared._Funkystation.Footprints;
using Content.Shared._Moffstation.Snapshots;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Camera;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.MapText;
using Content.Shared.Mind;
using Content.Shared.Parallax;
using Content.Shared.Photography;
using Content.Shared.Sprite;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Snapshots;

/// <summary>
/// Prints snapshots from <see cref="SnapshotCaptureComponent"/> items, records what the photographer could see
/// and ensures no arbitary data is send
/// </summary>
public sealed partial class ServerSnapshotCaptureSystem : SnapshotCaptureSystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedRoofSystem _roof = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SnapshotPersistenceSystem _snapshotPersistence = default!;
    [Dependency] private MindSystem _mindSystem = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    [Dependency] private EntityQuery<EyeCursorOffsetComponent> _cursorOffsetQuery = default!;
    [Dependency] private EntityQuery<EyeComponent> _eyeQuery = default!;
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<MapLightComponent> _mapLightQuery = default!;
    [Dependency] private EntityQuery<ParallaxComponent> _parallaxQuery = default!;
    [Dependency] private EntityQuery<RoofComponent> _roofQuery = default!;
    [Dependency] private EntityQuery<ImplicitRoofComponent> _implicitRoofQuery = default!;
    [Dependency] private EntityQuery<OccluderComponent> _occluderQuery = default!;
    [Dependency] private EntityQuery<BuckleComponent> _buckleQuery = default!;
    [Dependency] private EntityQuery<StealthComponent> _stealthQuery = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;
    [Dependency] private EntityQuery<VirtualItemComponent> _virtualItemQuery = default!;
    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery = default!;
    [Dependency] private EntityQuery<MapTextComponent> _mapTextQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _transformQuery = default!;
    [Dependency] private EntityQuery<MindComponent> _mindQuery = default!;

    private const int MaxEntities = 256;

    private const int MaxNearbyLights = 64;

    private const int MaxOccluders = 512;

    /// <summary>
    /// How far the photographer can see when deciding which tiles made it into the shot.
    /// </summary>
    private const float SightRange = 20f;

    /// <summary>
    /// Stealthed entities fainter than this are left out of the snapshot.
    /// </summary>
    private const float MinStealthVisibility = 0.5f;

    private static readonly Vector2i[] Neighbours = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    /// <summary>
    /// Points within a tile checked for line of sight: the middle, edge midpoints and corners. Kept inside the tile so
    /// a ray grazing a wall's corner can't count the tile behind it as seen.
    /// </summary>
    private static readonly Vector2[] TileSamples =
    [
        new(0.5f, 0.5f),
        new(0.5f, 0.15f), new(0.5f, 0.85f), new(0.15f, 0.5f), new(0.85f, 0.5f),
        new(0.15f, 0.15f), new(0.85f, 0.15f), new(0.15f, 0.85f), new(0.85f, 0.85f),
    ];

    private readonly HashSet<EntityUid> _found = new();

    private readonly Dictionary<EntityUid, int> _captured = new();

    private readonly HashSet<EntityUid> _supportCaptured = new();

    private readonly HashSet<Entity<PointLightComponent>> _nearbyLights = [];

    private readonly HashSet<Vector2i> _supportTiles = [];

    /// <summary>
    /// The viewfinder's extended view needs a larger PVS range, like binoculars.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnGetPvsScale(Entity<SnapshotCaptureComponent> ent, ref HeldRelayedEvent<GetEyePvsScaleRelayedEvent> args)
    {
        if (ent.Comp.Aiming && _cursorOffsetQuery.TryComp(ent, out var cursorOffset))
            args.Args.Scale += cursorOffset.PvsIncrease;
    }

    [SubscribeLocalEvent]
    private void OnCaptured(Entity<SnapshotCaptureComponent> ent, ref SnapshotCapturedEvent args)
    {
        if (Capture(args.User, args.Center, ent.Comp.HalfExtents) is not { } data)
            return;

        if (args.Focus is { } focus && _captured.ContainsKey(focus))
        {
            data.SubjectNameText = Loc.GetString(
                HasComp<PhotographComponent>(focus)
                    ? "photograph-name-text-photograph"
                    : "photograph-name-text",
                ("entity", Identity.Entity(focus, EntityManager)));
            data.SubjectDescription = HasComp<PhotographComponent>(focus)
                ? null
                : _examine.GetExamineText(focus, args.User)?.ToMarkup();
        }

        var snapshot = Spawn(ent.Comp.Snapshot, Transform(ent).Coordinates);
        EnsureComp<SnapshotComponent>(snapshot, out var comp);
        comp.Data = data;

        if (!_mindSystem.TryGetMind(args.User, out var mind, out _) || !_mindQuery.TryComp(mind, out var mindComp) || !_snapshotPersistence.SaveData((snapshot, comp), data, (mind, mindComp)))
        {
            Del(snapshot);
            _popup.PopupEntity(Loc.GetString("snapshot-capture-failed"), args.User, args.User);
            return;
        }

        _hands.PickupOrDrop(args.User, snapshot, dropNear: true);
    }

    /// <summary>
    /// Records Lighting and such around the Photographer
    /// </summary>
    public SnapshotData? Capture(EntityUid viewer, MapCoordinates shotCenter, Vector2i halfExtents)
    {
        var viewerCoords = TransformSystem.GetMapCoordinates(viewer);
        var mapId = shotCenter.MapId;
        if (mapId == MapId.Nullspace || viewerCoords.MapId != mapId)
            return null;

        var visMask = _eyeQuery.TryComp(viewer, out var eye) ? eye.VisibilityMask : EyeComponent.DefaultVisibilityMask;

        var snapshot = new SnapshotData { HalfExtents = halfExtents };

        // Same frame the viewfinder showed, so the snapshot matches what was aimed at.
        var frame = GetFrame(shotCenter, halfExtents);
        Entity<MapGridComponent>? grid = frame.Grid is { } gridUid && _gridQuery.TryComp(gridUid, out var gridComp)
            ? (gridUid, gridComp)
            : null;
        var frameRotation = frame.Rotation;
        var frameToWorld = frame.FrameToWorld;
        var worldToFrame = frame.WorldToFrame;
        var centerTile = frame.CenterTile;
        var center = centerTile + new Vector2(0.5f);

        // The snapshot only has the tiles that made it into the shot, and anything without a tile gets the map's ambient light.
        snapshot.AmbientLight = grid is { } roofGrid && GetRoof(roofGrid, centerTile) is { } indoorRoof
            ? indoorRoof
            : _mapLightQuery.TryComp(_map.GetMapOrInvalid(mapId), out var mapLight)
                ? mapLight.AmbientLightColor
                : Color.Black;

        // A tile is in the snapshot if the photographer can see any part of it, not just its middle, so partly visible
        // tiles next to walls aren't blacked out.
        var visible = new HashSet<Vector2i>();
        for (var x = -halfExtents.X; x <= halfExtents.X; x++)
        {
            for (var y = -halfExtents.Y; y <= halfExtents.Y; y++)
            {
                var offset = new Vector2i(x, y);
                var index = centerTile + offset;
                if (CanSeeAnyOfTile(viewerCoords, index, frameToWorld, worldToFrame, mapId))
                    visible.Add(offset);
            }
        }

        // Walls seen from the side add their own tile, so they have floor to anchor to and a roof over them.
        var tilesInShot = new HashSet<Vector2i>(visible);

        _found.Clear();
        _captured.Clear();
        _supportCaptured.Clear();
        var shotBounds = new Box2(centerTile - snapshot.HalfExtents, centerTile + snapshot.HalfExtents + Vector2i.One);
        var frameBox = shotBounds.Enlarged(1f);
        if (grid is { } lookupGrid)
            _lookup.GetLocalEntitiesIntersecting(lookupGrid, frameBox, _found, LookupFlags.Uncontained);
        else
            _found.UnionWith(_lookup.GetEntitiesIntersecting(mapId, frameBox, LookupFlags.Uncontained));

        var supportTiles = _supportTiles;
        supportTiles.Clear();
        foreach (var uid in _found)
        {
            if (snapshot.Entities.Count >= MaxEntities)
                break;

            if (!ShouldInclude(uid, visMask, grid?.Owner) || MetaData(uid).EntityPrototype is not { } proto)
                continue;

            var xform = Transform(uid);
            var local = Vector2.Transform(TransformSystem.GetWorldPosition(xform), worldToFrame);
            var tileOffset = TileOf(local) - centerTile;
            if (Math.Abs(tileOffset.X) > halfExtents.X || Math.Abs(tileOffset.Y) > halfExtents.Y)
                continue;

            if (!visible.Contains(tileOffset))
            {
                if (!_occluderQuery.HasComp(uid) || !HasVisibleNeighbour(tileOffset, visible))
                    continue;

                tilesInShot.Add(tileOffset);
            }

            _captured[uid] = snapshot.Entities.Count;
            snapshot.Entities.Add(new SnapshotEntity
            {
                Prototype = proto.ID,
                Offset = local - center,
                Rotation = TransformSystem.GetWorldRotation(xform) - frameRotation,
                Appearance = GetAppearance(uid),
                Light = GetLight(uid),
                MapText = GetMapText(uid),
                Humanoid = GetHumanoid(uid, visMask),
                Footprint = GetFootprint(uid),
            });
        }

        var worldCenter = Vector2.Transform(center, frameToWorld);

        CaptureNearbyLights(snapshot, mapId, worldCenter, centerTile, shotBounds, worldToFrame,
            frameRotation, viewerCoords, visMask, supportTiles);

        foreach (var (uid, index) in _captured)
        {
            if (_buckleQuery.TryComp(uid, out var buckle)
                && buckle.BuckledTo is { } strap
                && _captured.TryGetValue(strap, out var strapIndex))
            {
                snapshot.Entities[index].BuckledTo = strapIndex;
            }
        }

        if (grid is { } g)
        {
            foreach (var offset in tilesInShot)
            {
                var index = centerTile + offset;
                if (!_map.TryGetTileRef(g, g.Comp, index, out var tileRef)
                    || tileRef.Tile.IsEmpty
                    || !_tileDef.TryGetDefinition(tileRef.Tile.TypeId, out var tileDef))
                {
                    continue;
                }

                snapshot.Tiles.Add(new SnapshotTile(offset,
                    tileDef.ID,
                    tileRef.Tile.Variant,
                    tileRef.Tile.RotationMirroring,
                    GetRoof(g, index)));
            }
        }

        for (var x = -halfExtents.X; x <= halfExtents.X; x++)
        {
            for (var y = -halfExtents.Y; y <= halfExtents.Y; y++)
            {
                var offset = new Vector2i(x, y);
                if (!tilesInShot.Contains(offset))
                    snapshot.Hidden.Add(offset);
            }
        }

        snapshot.WorldCenter = worldCenter;
        snapshot.Parallax = _parallaxQuery.TryComp(_map.GetMapOrInvalid(mapId), out var parallax)
            ? parallax.Parallax
            : null;

        var viewerLocal = Vector2.Transform(viewerCoords.Position, worldToFrame);
        snapshot.Viewer = viewerLocal - center;
        if (grid is { } occluderGrid)
            AddSeenOccluders(snapshot, occluderGrid, viewerCoords, viewerLocal, centerTile, frameToWorld, worldToFrame);

        return snapshot;
    }

    /// <summary>
    /// Captures off-frame lights that still illuminate the frame as support-only entities.
    /// Reuses <see cref="_nearbyLights"/> instead of allocating a fresh lookup list per shot.
    /// </summary>
    private void CaptureNearbyLights(
        SnapshotData snapshot,
        MapId mapId,
        Vector2 worldCenter,
        Vector2i centerTile,
        Box2 frameBox,
        Matrix3x2 worldToFrame,
        Angle frameRotation,
        MapCoordinates _,
        int visMask,
        HashSet<Vector2i> supportTiles)
    {
        var maxLightRadius = _cfg.GetCVar(CVars.MaxLightRadius);
        if (!float.IsFinite(maxLightRadius) || maxLightRadius <= 0f)
            return;

        var frameRadius = new Vector2(snapshot.HalfExtents.X + 0.5f, snapshot.HalfExtents.Y + 0.5f).Length();
        _nearbyLights.Clear();
        _lookup.GetEntitiesInRange(new MapCoordinates(worldCenter, mapId),
            frameRadius + maxLightRadius + 1f,
            _nearbyLights);

        var center = Vector2.Transform(worldCenter, worldToFrame);
        var capturedCount = 0;
        foreach (var incompleteLight in _nearbyLights)
        {
            var uid = incompleteLight.Owner;
            if (capturedCount >= MaxNearbyLights)
            {
                if (capturedCount == MaxNearbyLights)
                    Log.Warning($"Snapshot capture truncated at {MaxNearbyLights} nearby lights.");
                break;
            }

            if (!_transformQuery.TryComp(uid, out var transform))
                continue;

            Entity<PointLightComponent,TransformComponent> light = (incompleteLight.Owner, incompleteLight.Comp, transform);

            if (_captured.ContainsKey(uid) || _supportCaptured.Contains(uid))
                continue;

            var entityWorldPosition = TransformSystem.GetWorldPosition(light.Comp2);
            var worldRotation = TransformSystem.GetWorldRotation(light.Comp2);
            var lightWorldPosition = entityWorldPosition + worldRotation.RotateVec(light.Comp1.Offset);
            var lightLocalPosition = Vector2.Transform(lightWorldPosition, worldToFrame);
            var closest = Vector2.Clamp(lightLocalPosition, frameBox.BottomLeft, frameBox.TopRight);
            var radius = Math.Min(light.Comp1.Radius, maxLightRadius);
            if (!float.IsFinite(radius)
                || radius <= 0f
                || Vector2.DistanceSquared(lightLocalPosition, closest) > radius * radius)
            {
                continue;
            }

            if (!ShouldIncludeSupportLight(uid, visMask)
                || MetaData(uid).EntityPrototype is not { } proto)
            {
                continue;
            }

            // Support-only lights do not need to be directly visible to the viewer: they are rebuilt as
            // invisible illumination sources, so a partition that hides the lamp itself does not prevent it
            // from contributing light to the visible frame.Expand commentComment on lines R843 to R845Resolved

            snapshot.Entities.Add(new SnapshotEntity
            {
                Prototype = proto.ID,
                Offset = Vector2.Transform(entityWorldPosition, worldToFrame) - center,
                Rotation = worldRotation + frameRotation,
                SupportOnly = true,
                Light = new SnapshotLight(
                    light.Comp1.Enabled,
                    light.Comp1.Color,
                    light.Comp1.Radius,
                    light.Comp1.Energy,
                    light.Comp1.Softness,
                    light.Comp1.Falloff,
                    light.Comp1.CurveFactor,
                    light.Comp1.CastShadows,
                    light.Comp1.Offset),
            });

            _supportCaptured.Add(uid);
            supportTiles.Add(TileOf(Vector2.Transform(entityWorldPosition, worldToFrame)) - centerTile);

            capturedCount++;
        }
    }

    /// <summary>
    /// Records the sight-blocking tiles between the photographer and the frame, including ones just outside it that
    /// cast shadows into it, so the viewer can draw shadows like the game's field of view.
    /// </summary>
    private void AddSeenOccluders(SnapshotData snapshot,
        Entity<MapGridComponent> grid,
        MapCoordinates viewer,
        Vector2 viewerLocal,
        Vector2i centerTile,
        Matrix3x2 frameToWorld,
        Matrix3x2 worldToFrame)
    {
        var frameBox = new Box2(centerTile - snapshot.HalfExtents, centerTile + snapshot.HalfExtents + Vector2i.One);
        var area = frameBox.ExtendToContain(viewerLocal).Enlarged(1f);

        _found.Clear();
        _lookup.GetLocalEntitiesIntersecting(grid, area, _found, LookupFlags.Static);

        var checkedTiles = new HashSet<Vector2i>();
        foreach (var uid in _found)
        {
            if (snapshot.Occluders.Count >= MaxOccluders)
                break;

            if (!_occluderQuery.TryComp(uid, out var occluder) || !occluder.Enabled)
                continue;

            var tile = TileOf(uid, worldToFrame);
            if (!checkedTiles.Add(tile))
                continue;

            if (CanSeeAnyOfTile(viewer, tile, frameToWorld, worldToFrame, viewer.MapId))
                snapshot.Occluders.Add(tile - centerTile);
        }
    }

    private bool ShouldInclude(EntityUid uid, int visMask, EntityUid? grid)
    {
        if (uid == grid || _mapQuery.HasComp(uid) || _gridQuery.HasComp(uid))
            return false;

        if (!IsVisibleTo(uid, visMask))
            return false;

        return !_container.IsEntityOrParentInContainer(uid);
    }

    /// <summary>
    /// Support-only lights may be inside non-occluding containers, such as a PDA flashlight held in hand.
    /// The illumination index already drops lights hidden in occluding containers, so only the visibility
    /// mask matters here.
    /// </summary>
    private bool ShouldIncludeSupportLight(EntityUid uid, int visMask)
        => !_mapQuery.HasComp(uid)
           && !_gridQuery.HasComp(uid)
           && IsVisibleTo(uid, visMask);

    /// <summary>
    /// PVS layer rules to not accidentily capture ghosts and such
    /// </summary>
    private bool IsVisibleTo(EntityUid uid, int visMask)
    {
        var meta = MetaData(uid);
        if ((visMask & meta.VisibilityMask) != meta.VisibilityMask)
            return false;

        return !_stealthQuery.TryComp(uid, out var stealth)
            || !stealth.Enabled
            || _stealth.GetVisibility(uid, stealth) >= MinStealthVisibility;
    }

    private Color? GetRoof(Entity<MapGridComponent> grid, Vector2i index)
    {
        if (_roofQuery.TryComp(grid, out var roof))
            return _roof.GetColor((grid, grid.Comp, roof), index);

        return _implicitRoofQuery.TryComp(grid, out var implicitRoof) ? implicitRoof.Color : null;
    }

    /// <summary>
    /// Records a character´s apperance
    /// </summary>
    private SnapshotHumanoid? GetHumanoid(EntityUid uid, int visMask)
    {
        if (!_humanoidQuery.TryComp(uid, out var profile)
            || !_visualBody.TryGatherMarkingsData(uid, null, out var profiles, out _, out var markings))
        {
            return null;
        }

        var humanoid = new SnapshotHumanoid
        {
            Species = profile.Species,
            Profiles = profiles,
            Markings = markings,
        };

        if (_inventory.TryGetSlots(uid, out var slots))
        {
            foreach (var slot in slots)
            {
                // Pockets and the like don't show on the sprite, so their contents stay private even if someone
                // tried to read image data elicitly.
                if (slot.StripHidden || !_inventory.TryGetSlotEntity(uid, slot.Name, out var worn))
                    continue;

                if (GetItem(worn.Value, visMask) is { } item)
                    humanoid.Worn[slot.Name] = item;
            }
        }

        foreach (var hand in _hands.EnumerateHands(uid))
        {
            if (!_hands.TryGetHeldItem(uid, hand, out var held)
                || _virtualItemQuery.HasComp(held)
                || GetItem(held.Value, visMask) is not { } item)
            {
                continue;
            }

            humanoid.Held[hand] = item;
        }

        return humanoid;
    }

    private SnapshotItem? GetItem(EntityUid uid, int visMask)
    {
        if (!IsVisibleTo(uid, visMask) || MetaData(uid).EntityPrototype is not { } proto)
            return null;

        return new SnapshotItem(proto.ID, GetAppearance(uid), GetRandomSprite(uid));
    }

    /// <summary>
    /// Copies the RandomSprite selection so greyscale tints (tools etc.) rebuild with the same color.
    /// </summary>
    private Dictionary<string, SpriteSelection>? GetRandomSprite(EntityUid uid) // Persistence: (string State, Color? Color) -> SpriteSelection
    {
        if (!TryComp<RandomSpriteComponent>(uid, out var component) || component.Selected.Count == 0)
            return null;

        // Persistence Start
        var dict = new Dictionary<string, SpriteSelection>(component.Selected.Count);
        foreach (var (key, value) in component.Selected)
        {
            dict[key] = new SpriteSelection(value.State, value.Color);
        }
        return dict;
        // Persistence End
    }

    /// <summary>
    /// Copies the appearance data the server already networks
    /// </summary>
    private AppearanceData? GetAppearance(EntityUid uid) // Persistence: Dictionary<Enum, object> -> AppearanceData
    {
        if (!_appearanceQuery.TryComp(uid, out var appearance) || !appearance.NetSyncEnabled)
            return null;

        if (EntityManager.GetComponentState(EntityManager.EventBus, appearance, null, GameTick.Zero)
                is not AppearanceComponentState { Data.Count: > 0 } state)
        {
            return null;
        }

        return new AppearanceData(new Dictionary<Enum, object>(state.Data)); // Persistence: Add AppearanceData(new
    }

    /// <summary>
    /// Copies the footprint layers and tint.
    /// </summary>
    private FootprintState? GetFootprint(EntityUid uid) // Persistence: FootprintComponent.FootprintComponent_AutoState -> FootprintState
    {
        if (!TryComp<FootprintComponent>(uid, out var footprint))
            return null;

        // Persistence Start
        var state = EntityManager.GetComponentState(EntityManager.EventBus, footprint, null, GameTick.Zero);

        if (state is FootprintComponent.FootprintComponent_AutoState footprintState)
        {
            return new FootprintState(footprintState);
        }

        return null;
        // Persistence End
    }

    private MapTextComponentState? GetMapText(EntityUid uid)
    {
        if (!_mapTextQuery.TryComp(uid, out var mapText))
            return null;

        return EntityManager.GetComponentState(EntityManager.EventBus, mapText, null, GameTick.Zero) as MapTextComponentState;
    }

    private SnapshotLight? GetLight(EntityUid uid)
    {
        if (!_light.TryGetLight(uid, out var light))
            return null;

        return new SnapshotLight(light.Enabled,
            light.Color,
            light.Radius,
            light.Energy,
            light.Softness,
            light.Falloff,
            light.CurveFactor,
            light.CastShadows,
            light.Offset);
    }

    private bool CanSeeAnyOfTile(MapCoordinates viewer, Vector2i index, Matrix3x2 frameToWorld, Matrix3x2 worldToFrame, MapId mapId)
    {
        foreach (var sample in TileSamples)
        {
            var point = new MapCoordinates(Vector2.Transform(index + sample, frameToWorld), mapId);

            // Whatever stands on the tile itself shouldn't hide it.
            if (_examine.InRangeUnOccluded(viewer,
                    point,
                    SightRange,
                    (Index: index, WorldToFrame: worldToFrame),
                    (uid, state) => TileOf(uid, state.WorldToFrame) == state.Index))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2i TileOf(EntityUid uid, Matrix3x2 worldToFrame)
    {
        return TileOf(Vector2.Transform(TransformSystem.GetWorldPosition(uid), worldToFrame));
    }

    private static Vector2i TileOf(Vector2 local)
    {
        return new Vector2i((int) MathF.Floor(local.X), (int) MathF.Floor(local.Y));
    }

    private static bool HasVisibleNeighbour(Vector2i tile, HashSet<Vector2i> visible)
    {
        foreach (var dir in Neighbours)
        {
            if (visible.Contains(tile + dir))
                return true;
        }

        return false;
    }
}
