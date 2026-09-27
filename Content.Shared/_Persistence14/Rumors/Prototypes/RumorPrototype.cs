using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared._Persistence14.Rumors.Prototypes;

[Prototype]
public sealed partial class RumorPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; set; } = string.Empty;

    [DataField]
    public string Description { get; set; } = string.Empty;

    [DataField]
    public List<ResPath> EventGrids { get; set; } = new List<ResPath>();

    [DataField]
    public CompletionType CompletionType { get; set; } = CompletionType.Discover;

    [DataField]
    public int GridsToSpawn { get; set; } = 1;
    [DataField]
    public float SpawnDistance { get; set; } = 500f;

    [DataField]
    public int CashReward { get; set; } = 0;
    [DataField]
    public int ReputationReward { get; set; } = 0;

}
public enum CompletionType
{
    Discover,
    Exterminate,
    Power,
    Rescue,
    Move

}


[DataDefinition]
[Serializable]
[Virtual]
public partial class ActiveRumor
{
    [DataField("_name")]
    public string Name = "Unnamed Rumor";
    [DataField]
    public string Description = "No description provided.";

    [DataField]
    public MapCoordinates? TargetPosition;

    [DataField]
    public bool ShouldSpawn = false;

    [DataField]
    public CompletionType CompletionType = CompletionType.Discover;

    [DataField]
    public ProtoId<MetaFactionPrototype> Faction = "Zenith";

    [DataField]
    public List<ResPath> EventGrids { get; set; } = new List<ResPath>();

    [DataField]
    public int GridsToSpawn { get; set; } = 1;
    [DataField]
    public float SpawnDistance { get; set; } = 500f;
    [DataField]
    public List<string> Targets { get; set; } = new List<string>();
    [DataField]
    public int CashReward { get; set; } = 2500;
    [DataField]
    public int ReputationReward { get; set; } = 50;

}
