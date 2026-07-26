using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Prototypes;

/// <summary>
/// A weighted table of possible outcomes for a polymorph-inflicting anomaly.
/// </summary>
[Prototype]
public sealed partial class PolymorphAnomalyTablePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// All possible polymorph outcomes this table can roll, along with their weight
    /// and how long each one lasts.
    /// </summary>
    [DataField(required: true)]
    public List<PolymorphAnomalyOption> Options = new();
}

/// <summary>
/// A single entry in a <see cref="PolymorphAnomalyTablePrototype"/>.
/// </summary>
[DataDefinition]
public sealed partial class PolymorphAnomalyOption
{
    /// <summary>
    /// The polymorph that will be applied if this option is picked.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;

    /// <summary>
    /// Relative weight of this option when randomly picking from the table.
    /// Weights don't need to add up to any particular total - they're relative to each other.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// The minimum amount of time a victim will remain polymorphed when this option is picked.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan MinDuration = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The maximum amount of time a victim will remain polymorphed when this option is picked.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan MaxDuration = TimeSpan.FromMinutes(5);
}
