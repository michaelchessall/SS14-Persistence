using System.Numerics;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised by the client when a damping mode has been selected
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleConsoleWaypointMessage : BoundUserInterfaceMessage
{
    public Vector2? Coords;
}
