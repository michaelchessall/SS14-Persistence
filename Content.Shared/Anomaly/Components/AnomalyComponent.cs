using Content.Shared.Anomaly.Effects;
using Content.Shared.Anomaly.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Numerics;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// This is used for tracking the general behavior of anomalies.
/// This doesn't contain the specific implementations for what
/// they do, just the generic behaviors associated with them.
///
/// Anomalies and their related components were designed here: https://hackmd.io/@ss14-design/r1sQbkJOs
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedAnomalySystem), typeof(SharedInnerBodyAnomalySystem))]
public sealed partial class AnomalyComponent : Component
{
    /// <summary>
    /// How likely an anomaly is to grow more dangerous. Moves both up and down.
    /// Ranges from 0 to 1.
    /// Values less than 0.5 indicate stability, whereas values greater
    /// than 0.5 indicate instability, which causes increases in severity.
    /// </summary>
    /// <remarks>
    /// Note that this doesn't refer to stability as a percentage: This is an arbitrary
    /// value that only matters in relation to the <see cref="GrowthThreshold"/> and <see cref="DecayThreshold"/>
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [DataField]
    public float Stability = 0f;

    /// <summary>
    /// How severe the effects of an anomaly are. Moves only upwards.
    /// Ranges from 0 to 1.
    /// A value of 0 indicates effects of extrememly minimal severity, whereas greater
    /// values indicate effects of linearly increasing severity.
    /// </summary>
    /// <remarks>
    /// Wacky-Stability scale lives on in my heart. - emo
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [DataField]
    public float Severity = 0f;

    #region Health
    /// <summary>
    /// The internal "health" of an anomaly.
    /// Ranges from 0 to 1.
    /// When the health of an anomaly reaches 0, it is destroyed without ever
    /// reaching a supercritical point.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [DataField]
    public float Health = 1f;

    /// <summary>
    /// If the <see cref="Stability"/> of the anomaly exceeds this value, it
    /// becomes too unstable to support itself and starts decreasing in <see cref="Health"/>.
    /// </summary>
    [DataField("decayhreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float DecayThreshold = 0.15f;

    /// <summary>
    /// The amount of health lost when the stability is below the <see cref="DecayThreshold"/>
    /// </summary>
    [DataField("healthChangePerSecond"), ViewVariables(VVAccess.ReadWrite)]
    public float HealthChangePerSecond = -0.01f;
    #endregion

    #region Growth
    /// <summary>
    /// If the <see cref="Stability"/> of the anomaly exceeds this value, it
    /// becomes unstable and starts increasing in <see cref="Severity"/>.
    /// </summary>
    [DataField("growthThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float GrowthThreshold = 0.5f;

    /// <summary>
    /// A coefficient used for calculating the increase in severity when above the GrowthThreshold
    /// </summary>
    [DataField("severityGrowthCoefficient"), ViewVariables(VVAccess.ReadWrite)]
    public float SeverityGrowthCoefficient = 0.07f;
    #endregion

    #region Pulse
    /// <summary>
    /// The time at which the next artifact pulse will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPulseTime = TimeSpan.Zero;

    /// <summary>
    /// The minimum interval between pulses.
    /// </summary>
    [DataField]
    public TimeSpan MinPulseLength = TimeSpan.FromMinutes(2);

    /// <summary>
    /// The maximum interval between pulses.
    /// </summary>
    [DataField]
    public TimeSpan MaxPulseLength = TimeSpan.FromMinutes(4);

    /// <summary>
    /// A percentage by which the length of a pulse might vary.
    /// </summary>
    [DataField]
    public float PulseVariation = 0.1f;

    /// <summary>
    /// The range that an anomaly's stability can vary each pulse. Scales with severity.
    /// </summary>
    /// <remarks>
    /// This is more likely to trend upwards than donwards, because that's funny
    /// </remarks>
    [DataField]
    public Vector2 PulseStabilityVariation = new(-0.1f, 0.15f);

    /// <summary>
    /// The sound played when an anomaly pulses
    /// </summary>
    [DataField]
    public SoundSpecifier? PulseSound = new SoundCollectionSpecifier("RadiationPulse");

    /// <summary>
    /// The sound plays when an anomaly goes supercritical
    /// </summary>
    [DataField]
    public SoundSpecifier? SupercriticalSound = new SoundCollectionSpecifier("Explosion");

    /// <summary>
    /// The sound plays at the start of the animation when an anomaly goes supercritical
    /// </summary>
    [DataField]
    public SoundSpecifier? SupercriticalSoundAtAnimationStart;

    /// <summary>
    /// The length of the animation before it goes supercritical in seconds.
    /// </summary>
    ///
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SupercriticalDuration = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// If true, skips the stock scale-up/fade-out visual effect that normally plays for
    /// SupercriticalDuration while going supercritical - the sprite is left completely alone by
    /// the base game's own visual handling. Defaults to false, so every existing anomaly's
    /// behavior is unchanged; only anomalies that explicitly opt in (to play their own fully
    /// custom animation instead, likely driven off AnomalySupercriticalStartedEvent) set this.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool SkipSupercriticalAnimation;

    /// <summary>
    /// If true, this anomaly stops pulsing entirely once it has gone supercritical at least
    /// once, checked via the persistent AnomalyVisuals.Supercritical appearance flag (which is
    /// set once and never cleared). Defaults to false, so every existing anomaly's behavior is
    /// unchanged - this only matters for an anomaly that survives its own supercritical event
    /// (DeleteEntity: false), since normally an anomaly is long deleted before this could ever
    /// come up. Without this, such an anomaly would just keep pulsing forever afterward, which
    /// also has the side effect of repeatedly toggling AnomalyVisuals.IsPulsing and re-triggering
    /// any GenericVisualizer mappings on unrelated appearance keys, visibly resetting whatever
    /// animation is currently showing even though nothing about it should have changed.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool StopPulsingAfterSupercritical;

    /// <summary>
    /// If true, suppresses the automatic core drop that would otherwise always happen when this
    /// anomaly goes supercritical (via AnomalySupercriticalEvent.SpawnCore, read by EndAnomaly).
    /// Defaults to false, matching existing behavior for every anomaly that doesn't set it.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool SuppressCoreOnSupercritical;

    /// <summary>
    /// If set, the client directly switches the sprite's Animated layer to this state the moment
    /// AnomalyVisuals.Supercritical is set - fully replacing whatever animation would otherwise
    /// show (the stock scale/fade, unless SkipSupercriticalAnimation is also set, or the ordinary
    /// pulse state) with this anomaly's own custom "going critical" animation. No GenericVisualizer
    /// wiring needed - any anomaly gets this just by setting this one field in yaml.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? SupercriticalAnimationState;

    /// <summary>
    /// If set alongside SupercriticalAnimationState, how long that state plays before the client
    /// automatically switches to SupercriticalSettledState. If null, SupercriticalAnimationState
    /// (once applied) is never automatically changed away from.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? SupercriticalAnimationDuration;

    /// <summary>
    /// The state the client switches the Animated layer to once SupercriticalAnimationDuration
    /// has elapsed since SupercriticalAnimationState was applied. Ignored if
    /// SupercriticalAnimationDuration isn't also set.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? SupercriticalSettledState;

    /// <summary>
    /// If set (alongside SupercriticalSettledState), the state the Animated layer switches to
    /// while this anomaly is pulsing AFTER having settled from its supercritical animation -
    /// letting a surviving, still-pulsing anomaly show a different pulse animation post-crack
    /// than the one it used before. Reverts to SupercriticalSettledState when the pulse ends.
    /// Null (default) means post-settle pulses just keep showing SupercriticalSettledState,
    /// matching previous behavior. Only meaningful for an anomaly that survives supercritical
    /// (DeleteEntity: false) and keeps pulsing afterward (StopPulsingAfterSupercritical: false).
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? SupercriticalPulseState;

    /// <summary>
    /// If set, the client switches the Animated layer to this state the moment
    /// AnomalyVisuals.Dying is set, taking priority over Supercritical/SupercriticalSettled/pulse
    /// state - for an anomaly whose "ending" isn't the stock supercritical collapse but a later,
    /// content-defined death condition (e.g. the Eye anomaly, once its last thrall is gone - see
    /// EyeAnomalySystem). Null (default) means AnomalyVisuals.Dying has no visual effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? DeathAnimationState;

    /// <summary>
    /// How long DeathAnimationState plays before the entity that set AnomalyVisuals.Dying finishes
    /// whatever it does once that time elapses (e.g. EyeAnomalySystem deleting the anomaly and
    /// dropping CorePrototype). Purely a timing value read back by that content-specific system -
    /// the generic AnomalySystem/SharedAnomalySystem never act on Dying by themselves.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? DeathAnimationDuration;

    /// <summary>
    /// SERVER-ONLY runtime state, not a data field - set once, the moment this anomaly starts
    /// going supercritical, purely so the server can measure SupercriticalAnimationDuration
    /// against it later (see SharedAnomalySystem.Update). Clients never need to know this value
    /// directly - they just react to the AnomalyVisuals.SupercriticalSettled appearance flag,
    /// which the server sets once this duration has actually elapsed.
    /// </summary>
    public TimeSpan? SupercriticalStartedAt;

    /// <summary>
    /// If true, StartSupercriticalEvent refuses to run a second time for this anomaly, checked
    /// via the persistent AnomalyVisuals.Supercritical appearance flag (set once, never cleared).
    /// Defaults to false, so every existing anomaly's behavior is unchanged - a stock anomaly is
    /// always deleted as part of its first (and only) EndAnomaly call, so it physically can't
    /// trigger this a second time anyway. This only matters for an anomaly that survives via
    /// DeleteEntity: false, which can otherwise be pushed back to Severity 1 later (e.g. by more
    /// transformation particle hits) and replay its entire "going critical" sequence - including
    /// any custom SupercriticalAnimationState - again from scratch.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool PreventRepeatedSupercritical;

    /// <summary>
    /// If true, this anomaly ignores every anomalous particle collision entirely once it has
    /// gone supercritical at least once, checked via the persistent AnomalyVisuals.Supercritical
    /// appearance flag. Defaults to false, so every existing anomaly's behavior is unchanged -
    /// again, only relevant for an anomaly that survives via DeleteEntity: false, since a stock
    /// anomaly is deleted before this could matter. Without this, particle hits (stability/
    /// severity/health changes) keep affecting the anomaly forever afterward, including
    /// potentially pushing Severity back up to 1 and triggering StartSupercriticalEvent all over
    /// again - see PreventRepeatedSupercritical, which blocks that specific outcome directly, but
    /// this stops the particle hit from doing anything at all in the first place.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IgnoreParticleHitsAfterSupercritical;
    #endregion

    /// <summary>
    /// The range of initial values for stability
    /// </summary>
    /// <remarks>
    /// +/- 0.2 from perfect stability (0.5)
    /// </remarks>
    [DataField]
    public (float, float) InitialStabilityRange = (0.4f, 0.6f);

    /// <summary>
    /// The range of initial values for severity
    /// </summary>
    /// <remarks>
    /// Between 0 and 0.5, which should be all mild effects
    /// </remarks>
    [DataField]
    public (float, float) InitialSeverityRange = (0.1f, 0.5f);

    /// <summary>
    /// The particle type that increases the severity of the anomaly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AnomalousParticleType SeverityParticleType;

    /// <summary>
    /// The particle type that destabilizes the anomaly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AnomalousParticleType DestabilizingParticleType;

    /// <summary>
    /// The particle type that weakens the anomalys health.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AnomalousParticleType WeakeningParticleType;

    /// <summary>
    /// The particle type that change anomaly behaviour.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AnomalousParticleType TransformationParticleType;

    #region Points and Vessels
    /// <summary>
    /// The vessel that the anomaly is connceted to. Stored so that multiple
    /// vessels cannot connect to the same anomaly.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ConnectedVessel;

    /// <summary>
    /// The minimum amount of research points generated per second
    /// </summary>
    [DataField]
    public int MinPointsPerSecond = 10;

    /// <summary>
    /// The maximum amount of research points generated per second
    /// This doesn't include the point bonus for being unstable.
    /// </summary>
    [DataField]
    public int MaxPointsPerSecond = 70;

    /// <summary>
    /// The multiplier applied to the point value for the
    /// anomaly being above the <see cref="GrowthThreshold"/>
    /// </summary>
    [DataField]
    public float GrowingPointMultiplier = 1.5f;
    #endregion

    /// <summary>
    /// A prototype entity that appears when an anomaly supercrit collapse.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? CorePrototype;

    /// <summary>
    /// A prototype entity that appears when an anomaly decays.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? CoreInertPrototype;

    #region Behavior Deviations

    [DataField]
    public ProtoId<AnomalyBehaviorPrototype>? CurrentBehavior;

    /// <summary>
    /// Presumption of anomaly to change behavior. The higher the number, the higher the chance that the anomaly will change its behavior.
    /// </summary>
    [DataField]
    public float Continuity = 0f;

    /// <summary>
    /// Minimum contituty probability chance, that can be selected by anomaly on MapInit
    /// </summary>
    [DataField]
    public float MinContituty = 0.1f;

    /// <summary>
    /// Maximum contituty probability chance, that can be selected by anomaly on MapInit
    /// </summary>
    [DataField]
    public float MaxContituty = 1.0f;

    #endregion

    #region Floating Animation
    /// <summary>
    /// How long it takes to go from the bottom of the animation to the top.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("animationTime")]
    public float AnimationTime = 2f;

    /// <summary>
    /// How far it goes in any direction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")]
    public Vector2 FloatingOffset = new(0, 0);

    public readonly string AnimationKey = "anomalyfloat";
    #endregion

    [DataField]
    public bool DeleteEntity = true;
}

/// <summary>
/// Event raised at regular intervals on an anomaly to do whatever its effect is.
/// </summary>
/// <param name="Anomaly">The anomaly pulsing</param>
/// <param name="Stability"></param>
/// <param name="Severity"></param>
[ByRefEvent]
public readonly record struct AnomalyPulseEvent(EntityUid Anomaly, float Stability, float Severity, float PowerModifier);

/// <summary>
/// Event raised when an anomaly goes supercritical, right before EndAnomaly runs.
/// </summary>
/// <param name="Anomaly">The anomaly going supercritical</param>
/// <param name="PowerModifier">The power modifier from the anomaly's current behavior, if any</param>
[ByRefEvent]
public record struct AnomalySupercriticalEvent(EntityUid Anomaly, float PowerModifier)
{
    /// <summary>
    /// Whether EndAnomaly should spawn a core once this event finishes. Defaults to true,
    /// matching the existing behavior for every anomaly that doesn't touch this field - set it
    /// to false from a handler to suppress the core drop for this specific supercritical event
    /// (e.g. an anomaly whose "real" ending is decided by its own later logic instead).
    /// </summary>
    public bool SpawnCore = true;
}

/// <summary>
/// Event broadcast after an anomaly goes supercritical
/// </summary>
/// <param name="Anomaly">The anomaly being shut down.</param>
/// <param name="Supercritical">Whether or not the anomaly shut down passively or via a supercritical event.</param>
[ByRefEvent]
public readonly record struct AnomalyShutdownEvent(EntityUid Anomaly, bool Supercritical);

/// <summary>
/// Event raised the moment an anomaly BEGINS going supercritical (StartSupercriticalEvent) -
/// well before AnomalySupercriticalEvent, which doesn't fire until after
/// AnomalyComponent.SupercriticalDuration has elapsed and the stock wind-up animation (or
/// whatever replaces it, if AnomalyComponent.SkipSupercriticalAnimation is set) finishes. Added
/// specifically so content can start its own custom "going critical" visual immediately, in sync
/// with when the sequence actually begins, rather than only being able to react once it's over.
/// </summary>
[ByRefEvent]
public readonly record struct AnomalySupercriticalStartedEvent(EntityUid Anomaly);

/// <summary>
/// Event raised exactly once, the moment AnomalyComponent.SupercriticalAnimationDuration has
/// elapsed and AnomalyVisuals.SupercriticalSettled is set (see SharedAnomalySystem.Update).
/// Added so content can react to "the crack-open animation has finished" specifically, distinct
/// from AnomalySupercriticalStartedEvent (which fires at the very beginning of the sequence) -
/// useful for anything that should only happen once the anomaly has visually finished settling,
/// e.g. reaching out with a tether effect only after it's fully open rather than mid-transition.
/// </summary>
[ByRefEvent]
public readonly record struct AnomalySupercriticalSettledEvent(EntityUid Anomaly);

/// <summary>
/// Event broadcast when an anomaly's severity is changed.
/// </summary>
/// <param name="Anomaly">The anomaly being changed</param>
[ByRefEvent]
public readonly record struct AnomalySeverityChangedEvent(EntityUid Anomaly, float Stability, float Severity);

/// <summary>
/// Event broadcast when an anomaly's stability is changed.
/// </summary>
[ByRefEvent]
public readonly record struct AnomalyStabilityChangedEvent(EntityUid Anomaly, float Stability, float Severity);

/// <summary>
/// Event broadcast when an anomaly's health is changed.
/// </summary>
/// <param name="Anomaly">The anomaly being changed</param>
[ByRefEvent]
public readonly record struct AnomalyHealthChangedEvent(EntityUid Anomaly, float Health);

/// <summary>
/// Event broadcast when an anomaly's behavior is changed.
/// This is raised after the relevant components are applied
/// </summary>
[ByRefEvent]
public readonly record struct AnomalyBehaviorChangedEvent(EntityUid Anomaly, ProtoId<AnomalyBehaviorPrototype>? Old, ProtoId<AnomalyBehaviorPrototype>? New);

/// <summary>
/// Event of anomaly being affected by exotic particle.
/// Is raised when particle collides with artifact.
/// </summary>
[ByRefEvent]
public record struct AnomalyAffectedByParticleEvent(EntityUid Anomaly, EntityUid Particle);
