using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.CharacterSnapshot;

/// <summary>
/// Cosmetic "last seen" snapshot of a character, taken whenever their character file is saved (cryo, first spawn).
/// Used only to dress the lobby preview dummy; never treated as authoritative.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CharacterAppearanceSnapshot
{
    /// <summary>
    /// Prototype of the saved body. Used for the preview when <see cref="Species"/> is null (e.g. a borg chassis).
    /// </summary>
    [DataField]
    public string? Body;

    /// <summary>
    /// Species of the saved body, if it was humanoid. The preview spawns this species' doll.
    /// </summary>
    [DataField]
    public ProtoId<SpeciesPrototype>? Species;

    /// <summary>
    /// Per-organ profile data (skin/eye color, sex), captured from the body's visual organs.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> OrganProfiles = new();

    /// <summary>
    /// Per-organ applied markings (hair, facial hair, tattoos...), captured from the body's visual organs.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings = new();

    /// <summary>
    /// Inventory slot name -> equipped item prototype ID.
    /// </summary>
    [DataField]
    public Dictionary<string, string> Slots = new();

    /// <summary>
    /// Hand ID -> held item prototype ID.
    /// </summary>
    [DataField]
    public Dictionary<string, string> Hands = new();

    [DataField]
    public DateTime CapturedAt;
}

/// <summary>
/// Sent to a client when they enter the lobby. Character save name -> snapshot.
/// </summary>
[Serializable, NetSerializable]
public sealed class CharacterSnapshotsEvent(Dictionary<string, CharacterAppearanceSnapshot> snapshots) : EntityEventArgs
{
    public Dictionary<string, CharacterAppearanceSnapshot> Snapshots = snapshots;
}
