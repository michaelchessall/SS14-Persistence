using System.Numerics;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.MapText;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using FootprintStateBase = Content.Shared._Funkystation.Footprints.FootprintComponent.FootprintComponent_AutoState; // Persistence

namespace Content.Shared._Moffstation.Snapshots;

/// <summary>
/// Viewers rebuild this on a client-only map, so clients never upload image data.
/// </summary>
/// <remarks>
/// Positions are relative to the center of the middle tile of the frame.
/// </remarks>
[Serializable, NetSerializable]
[DataDefinition] // Persistence
public sealed partial class SnapshotData
{
    [DataField] // Persistence
    public Vector2i HalfExtents;

    [DataField] // Persistence
    public Color AmbientLight = Color.Black;

    /// <summary>
    /// The map's space background, or null for the default one.
    /// </summary>
    [DataField] // Persistence
    public string? Parallax;

    /// <summary>
    /// World position of the middle of the frame. The snapshot is rebuilt there so the background lines up.
    /// </summary>
    [DataField] // Persistence
    public Vector2 WorldCenter;

    [DataField] // Persistence
    public List<SnapshotTile> Tiles = new();

    [DataField] // Persistence
    public List<SnapshotEntity> Entities = new();

    /// <summary>
    /// Tiles in the frame the photographer couldn't see. They're drawn black,otherwise they would be the space backround
    /// </summary>
    [DataField] // Persistence
    public List<Vector2i> Hidden = new();

    /// <summary>
    /// Where the photographer stood, relative to the middle of the frame. Shadows are cast from here.
    /// </summary>
    [DataField] // Persistence
    public Vector2 Viewer;

    /// <summary>
    /// Tiles with something blocking sight (walls, closed doors) between the photographer and the frame, as offsets from
    /// the middle tile.
    /// </summary>
    [DataField] // Persistence
    public List<Vector2i> Occluders = new();

    /// <summary>
    /// Name text for the subject clicked through the viewfinder, if that entity was included in the shot.
    /// </summary>
    [DataField] // Persistence
    public string? SubjectNameText;

    /// <summary>
    /// Examine text for the subject, if any.
    /// </summary>
    [DataField] // Persistence
    public string? SubjectDescription;
}

/// <param name="Roof">Roof color if the tile is roofed; roofs keep the map's ambient light off indoor tiles.</param>
[Serializable, NetSerializable]
[DataRecord] // Persistence
public partial record struct SnapshotTile(
    Vector2i Offset,
    string TileId,
    byte Variant,
    byte RotationMirroring,
    Color? Roof);

[Serializable, NetSerializable]
[DataDefinition] // Persistence
public sealed partial class SnapshotEntity
{
    [DataField] // Persistence
    public EntProtoId Prototype;

    [DataField] // Persistence
    public Vector2 Offset;

    [DataField] // Persistence
    public Angle Rotation;

    /// <summary>
    /// This entity is outside the frame and only exists to provide nearby illumination.
    /// </summary>
    [DataField] // Persistence
    public bool SupportOnly;

    /// <summary>
    /// Copy Apperance so the Rebuild view matches
    /// </summary>
    [DataField] // Persistence
    public AppearanceData? Appearance; // Persistence: Dictionary<Enum, object> -> AppearanceData

    [DataField] // Persistence
    public SnapshotLight? Light;

    /// <summary>
    /// Map text set on the entity itself (e.g. dev map signs), which respawning the prototype would lose.
    /// </summary>
    [DataField] // Persistence
    public MapTextComponentState? MapText;

    /// <summary>
    /// Index in <see cref="SnapshotData.Entities"/> to ensure draw order is respected
    /// for example if someone is buckled to something they get drawn ontop
    /// </summary>
    [DataField] // Persistence
    public int? BuckledTo;

    /// <summary>
    /// Making a Dummy similar to the Lobby screen instead of spawning a real prototype
    /// </summary>
    [DataField] // Persistence
    public SnapshotHumanoid? Humanoid;

    // Persistence
    /// <summary>
    /// RandomSprite selection (greyscale sprite tinting like tools), applied to the rebuilt clone.
    /// </summary>
    [DataField]
    public Dictionary<string, SpriteSelection>? RandomSprite; // Persistence: (string State, Color? Color) -> SpriteSelection

    // Persistence
    /// <summary>
    /// Footprint layers and tint.
    /// </summary>
    [DataField]
    public FootprintState? Footprint; // Persistence: FootprintComponent.FootprintComponent_AutoState -> FootprintState
}

// Persistence
/// <summary>
/// Wrapper for appearance data to avoid generator bug with typeof(nullable reference type).
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct AppearanceData(Dictionary<Enum, object> Data);

// Persistence
/// <summary>
/// Wrapper for sprite selection to avoid generator bug with typeof(nullable reference type).
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct SpriteSelection(string State, Color? Color);

[Serializable, NetSerializable]
[DataRecord] // Persistence
public partial record struct SnapshotLight(
    bool Enabled,
    Color Color,
    float Radius,
    float Energy,
    float Softness,
    float Falloff,
    float CurveFactor,
    bool CastShadows,
    Vector2 Offset = default);

/// <summary>
/// To ensure Character Items Inhand items ETC is carried over aswell as markings and such
/// </summary>
[Serializable, NetSerializable]
[DataDefinition] // Persistence
public sealed partial class SnapshotHumanoid
{
    [DataField] // Persistence
    public ProtoId<SpeciesPrototype> Species;

    [DataField] // Persistence
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> Profiles = new();

    [DataField] // Persistence
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings = new();

    [DataField] // Persistence
    public Dictionary<string, SnapshotItem> Worn = new();

    [DataField] // Persistence
    public Dictionary<string, SnapshotItem> Held = new();
}

[Serializable, NetSerializable]
[DataRecord] // Persistence
public partial record struct SnapshotItem(EntProtoId Prototype, AppearanceData? Appearance, Dictionary<string, SpriteSelection>? RandomSprite);

// Persistence
/// <summary>
/// Wrapper for FootprintComponent_AutoState to avoid generator bug with typeof(nullable reference type).
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct FootprintState(FootprintStateBase? Value);
