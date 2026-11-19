using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.AnimalHusbandry;

/// <summary>
/// This is used for simple animal husbandry. Entities with this component,
/// given they are next to a particular entity that fulfills a whitelist,
/// can create several "child" entities.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ReproductiveComponent : Component
{
    /// <summary>
    /// The next time when breeding will be attempted.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextBreedAttempt;

    /// <summary>
    /// Minimum length between each attempt to breed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinBreedAttemptInterval = TimeSpan.FromMinutes(2); // Persistence: FromSeconds(45) -> FromMinutes(2)

    /// <summary>
    /// Maximum length between each attempt to breed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxBreedAttemptInterval = TimeSpan.FromMinutes(12); // Persistence: FromSeconds(60) -> FromMinutes(12)

    /// <summary>
    /// How close to a partner an entity must be in order to breed.
    /// Unrealistically long.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BreedRange = 3f;

    /// <summary>
    /// How many other entities with this component are allowed in range before we stop.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Capacity = 6;

    /// <summary>
    /// The chance that, on a given attempt,
    /// for each valid partner, the entity will breed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BreedChance = 0.15f;

    /// <summary>
    /// Entity prototypes for what type of
    /// offspring can be produced by this entity.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Offspring = default!;

    /// <summary>
    /// Whether or not this entity has bred successfully
    /// and will produce offspring imminently
    /// </summary>
    [DataField]
    public bool Gestating;

    /// <summary>
    /// When gestation will end.
    /// Null if <see cref="Gestating"/> is false
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)] // Persistence: Add TimeOffsetSerializer
    public TimeSpan? GestationEndTime;

    /// <summary>
    /// How long it takes the entity after breeding
    /// to produce offspring
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GestationDuration = TimeSpan.FromMinutes(120); // Persistence: 1.5 -> 120

    /// <summary>
    /// How much hunger is consumed when an entity
    /// gives birth. A balancing tool to require feeding.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HungerPerBirth = 75f;

    /// <summary>
    /// If the entity's hunger satiation is below this value, it cannot reproduce.
    /// </summary>
    [DataField]
    public SatiationValue? MinHungerThreshold = "Peckish"; // Persistence: Add default "Peckish"

    /// <summary>
    /// If the entity's thirst satiation is below this value, it cannot reproduce.
    /// </summary>
    [DataField]
    public SatiationValue? MinThirstThreshold = "Parched"; // Persistence: Add default "Parched"

    /// <summary>
    /// Popup shown when an entity gives birth.
    /// Configurable for things like laying eggs.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId BirthPopup = "reproductive-birth-popup";

    /// <summary>
    /// Whether or not the offspring should be made into "infants".
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool MakeOffspringInfant = true;

    /// <summary>
    /// An entity whitelist for what entities
    /// can be this one's partner.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist PartnerWhitelist = default!;

    // Persistence Start: Reproduction cooldown
    /// <summary>
    /// How long after a birth until reproduction attempts can be allowed again.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CooldownDuration = TimeSpan.FromMinutes(45);

    /// <summary>
    /// When the cooldown period will end. Null when cooldown is finished.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? CooldownFinishedAt;
    // Persistence End
}
