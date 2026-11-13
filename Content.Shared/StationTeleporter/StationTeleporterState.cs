using Content.Shared.StationTeleporter.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.StationTeleporter;

[Serializable, NetSerializable]
public enum StationTeleporterConsoleUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class StationTeleporterState(List<StationTeleporterStatus> teleporters, NetEntity? selected = null, NetEntity? console = null, NetCoordinates? consoleCoords = null)
    : BoundUserInterfaceState
{
    public NetEntity? SelectedTeleporter = selected;
    public NetEntity? Console = console;
    public NetCoordinates? ConsoleCoordinates = consoleCoords;
    public List<StationTeleporterStatus> Teleporters = teleporters;
}

[Serializable, NetSerializable]
public sealed class StationTeleporterStatus(
    NetEntity teleporterUid,
    NetCoordinates coordinates,
    NetCoordinates? link,
    NetEntity? chip,
    string name,
    bool powered,
    bool canConnect,
    bool canLocate)
{
    public NetEntity TeleporterUid = teleporterUid;
    public NetCoordinates? Coordinates = coordinates;
    public NetCoordinates? LinkCoordinates = link;
    public NetEntity? Chip = chip;
    public string Name = name;
    public bool Powered = powered;
    public bool CanConnect = canConnect;
    public bool CanLocate = canLocate;
}

/// <summary>
/// Sent when the client clicks on any active teleporter shown by a <see cref="StationTeleporterConsoleComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationTeleporterClickMessage(NetEntity? teleporter, NetEntity? chip) : BoundUserInterfaceMessage
{
    public NetEntity? Teleporter = teleporter;
    public NetEntity? Chip = chip;
}

[Serializable, NetSerializable]
public enum TeleporterPortalVisuals
{
    Color,
}
