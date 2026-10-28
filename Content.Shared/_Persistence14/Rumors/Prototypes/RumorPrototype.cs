using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Nutrition;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
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
    public float SpawnDistance { get; set; } = 50f;

    [DataField]
    public TimeSpan GridLifetime { get; set; } = TimeSpan.FromHours(2);
    [DataField]
    public bool DespawnGrids { get; set; } = true;
    [DataField]
    public int CashReward { get; set; } = 0;
    [DataField]
    public int ReputationReward { get; set; } = 0;
    [DataField]
    public List<ProtoId<CargoBountyPrototype>> PossibleBounties { get; set; } = new();
    [DataField]
    public string DescriptionAddon { get; set; } = "";

    [DataField]
    public List<ProtoId<FlavorPrototype>> PossibleFlavors = new();

    [DataField]
    public List<List<ProtoId<FlavorPrototype>>> PossibleFlavorGroups = new();

    [DataField]
    public int FlavorsToPick = 1;
    [DataField]
    public int CompletionTarget = 30;
    [DataField]
    public List<ProtoId<RumorPrototype>> PossibleFollowups = new();
    [DataField]
    public int RewardRumors = 0;

    [DataField]
    public string TargetTag = "";
    [DataField]
    public EntityTableSelector? Table;

}
public enum CompletionType
{
    Discover,
    Exterminate,
    Power,
    Rescue,
    Move,
    Bounty,
    Eat,
    Drink,
    Pray,
    ArtifactNodesUnlock,
    ReceiveMail,
    AnomalyStabilize

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
    public float SpawnDistance { get; set; } = 50f;
    [DataField]
    public List<string> Targets { get; set; } = new List<string>();
    [DataField]
    public int CashReward { get; set; } = 2500;
    [DataField]
    public int ReputationReward { get; set; } = 50;
    [DataField]
    public CargoBountyData? Bounty = null;
    [DataField]
    public List<ProtoId<FlavorPrototype>> TargetFlavors = new();
    [DataField]
    public List<List<ProtoId<FlavorPrototype>>> TargetFlavorGroups = new();
    [DataField]
    public int CompletionProgress = 0;
    [DataField]
    public ProtoId<RumorPrototype> OriginalPrototype;
    [DataField]
    public bool DebugComplete = false;
    [DataField]
    public string TargetStation = "";
    [DataField]
    public string OriginalOwner = "";
    [DataField]
    public bool Transfer = false;
    [DataField]
    public bool Cancelable = false;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CancelTime = TimeSpan.Zero;


    public ActiveRumor(ProtoId<RumorPrototype> originalPrototype)
    {
        OriginalPrototype = originalPrototype;
    }
}
