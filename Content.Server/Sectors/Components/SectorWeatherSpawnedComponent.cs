using System.Numerics;
using Content.Server.Sectors.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Sectors.Components;

/// <summary>
/// Tracks entities spawned by sector weather so they can be cleaned up if they stray too far.
/// </summary>
[RegisterComponent, Access(typeof(SectorWeatherSpawnSystem))]
public sealed partial class SectorWeatherSpawnedComponent : Component
{
    [DataField]
    [ViewVariables]
    public Vector2 SpawnWorldPosition;

    [DataField]
    [ViewVariables]
    public float MaxDistanceFromSpawn;

    [DataField]
    [ViewVariables]
    public string WeatherId = string.Empty;

    [DataField]
    [ViewVariables]
    public EntProtoId SpawnPrototype = string.Empty;
}
