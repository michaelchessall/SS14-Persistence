using Content.Shared._Persistence14.PersistentIdentifier.Reference;
using Content.Shared._Persistence14.PersistentIdentifier;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.StationTeleporter.Components;

/// <summary>
/// Stores a reference to a specific teleporter. Can be inserted into the teleporter control console so that the console can control this teleporter.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedStationTeleporterSystem))]
public sealed partial class TeleporterChipComponent : Component
{
    /// <summary>
    /// Uid of the teleporter this chip is synced with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PersistentEntityReference ConnectedTeleporter = PersistentIdentifierSystem.EmptyId;

    /// <summary>
    /// The already-localized name of the teleporter this chip is synced with, copied here for use when examining the chip.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ConnectedName = string.Empty;

    /// <summary>
    /// The sound played when this chip records a teleporter's coordinates.
    /// </summary>
    [DataField]
    public SoundSpecifier RecordSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };

    /// <summary>
    /// The maximum distance a portal may be from the console in order to be controlled by the console.
    /// </summary>
    [DataField]
    public float MaxConnectDistance = 512f;

    /// <summary>
    /// The maximum distance at which the console can locate the connected portal.
    /// </summary>
    [DataField]
    public float MaxLocateDistance = 1024f;
}
