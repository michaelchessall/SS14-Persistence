using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.PersistentIdentifier.Reference;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Research.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResearchServerComponent : Component
{
    /// <summary>
    /// The amount of points on the server.
    /// </summary>
    [AutoNetworkedField]
    [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
    public int Points;

    [AutoNetworkedField]
    [DataField(readOnly: true)]
    public PersistentEntityReference LastKnownGrid = PersistentIdentifierSystem.EmptyId;

    [AutoNetworkedField]
    [DataField(readOnly: true)]
    public PersistentEntityReference LastKnownFaction = PersistentIdentifierSystem.EmptyId;

    [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;

    [DataField("researchConsoleUpdateTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ResearchConsoleUpdateTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// A set of multipliers applied to technologies based on their tier. Used when calculating the diversity multiplier for tech costs.
    /// </summary>
    [DataField]
    public float[] TierDiversityImpactMultipliers = [1, 2, 4];

    /// <summary>
    /// A set of multipliers applied to technologies as more disciplines are unlocked. Used when calculating the diversity multiplier for tech costs.
    /// </summary>
    [DataField]
    public float[] BreadthDiversityImpactMultipliers = [0.0f, 0.05f, 0.25f, .7f];

    [DataField]
    public float MaxDiversityPenalty = 4f;

    /// <summary>
    /// When a server is made on a grid/faction with an existing server, this server gets parented to that one and that server gets a list of children.
    /// </summary>
    [DataField(readOnly: true)]
    public PersistentEntityReference ParentServer = PersistentIdentifierSystem.EmptyId;

    public bool IsMain => ParentServer != PersistentIdentifierSystem.EmptyId;

    /// <summary>
    /// Servers which have this server as a parent.
    /// </summary>
    [DataField(readOnly: true)]
    public HashSet<PersistentEntityReference> ChildServers = new();
}

/// <summary>
/// Event raised on a server's clients when the point value of the server is changed.
/// </summary>
/// <param name="Server"></param>
/// <param name="Total"></param>
/// <param name="Delta"></param>
[ByRefEvent]
public readonly record struct ResearchServerPointsChangedEvent(EntityUid Server, int Total, int Delta);

/// <summary>
/// Event raised every second to calculate the amount of points added to the server.
/// </summary>
/// <param name="Server"></param>
/// <param name="Points"></param>
[ByRefEvent]
public record struct ResearchServerGetPointsPerSecondEvent(EntityUid Server, int Points);

