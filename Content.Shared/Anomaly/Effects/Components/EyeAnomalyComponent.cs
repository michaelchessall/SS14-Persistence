using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Effects.Components;

/// <summary>
/// The Eye anomaly. Three phases:
///
/// PULSE: on every normal pulse, grabs a number of alive creatures within range and shuffles them
/// into swapped pairs - see EyeAnomalySystem and MindSwapHomeComponent for how displaced minds
/// always find their way back to their true original body.
///
/// CRIT (tether, mind capture, and guardian AI): once the crack-open animation settles, nearby
/// mobs get tethered, their faction swapped to EyeThrall, and their mind captured into a vessel
/// (or given a MindShield grace period first) with a guardian AI (HTN) taking over the body - see
/// EyeAnomalySystem. The crack-open behavior itself (custom animation, no core drop, no more
/// pulsing afterward) is handled generically by the base AnomalyComponent/AnomalySystem - see
/// SupercriticalAnimationState, SuppressCoreOnSupercritical, etc. on that component instead of here.
///
/// DEATH: once every thrall tethered to this eye has escaped or died and none remain, the anomaly
/// plays its own death animation (AnomalyComponent.DeathAnimationState/DeathAnimationDuration,
/// same place as SupercriticalAnimationState and friends) and then destroys itself, dropping the
/// generic AnomalyComponent.CorePrototype - see the "Death" fields below and EyeAnomalySystem's
/// TryTetherNearby/BreakTether/Update.
/// </summary>
[RegisterComponent]
public sealed partial class EyeAnomalyComponent : Component
{
    // ================= Pulse phase (mind swap) =================

    /// <summary>Search radius at minimum (0) severity.</summary>
    [DataField(required: true)]
    public float MinRange = 2f;

    /// <summary>Search radius at maximum (1) severity.</summary>
    [DataField(required: true)]
    public float MaxRange = 5f;

    /// <summary>
    /// How many creatures get grabbed at minimum severity. Always rounded down to the nearest
    /// even number, since they're swapped in pairs.
    /// </summary>
    [DataField(required: true)]
    public int MinTargets = 2;

    /// <summary>How many creatures get grabbed at maximum severity. Always rounded down to the nearest even number.</summary>
    [DataField(required: true)]
    public int MaxTargets = 10;

    /// <summary>How long a swap lasts at minimum severity.</summary>
    [DataField(required: true)]
    public TimeSpan MinDuration = TimeSpan.FromSeconds(30);

    /// <summary>How long a swap lasts at maximum severity.</summary>
    [DataField(required: true)]
    public TimeSpan MaxDuration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// If true, a body entering Critical state immediately sends its current occupant mind home
    /// (cascading if that home is occupied) instead of waiting for the timer.
    /// </summary>
    [DataField]
    public bool RevertOnCrit = true;

    /// <summary>
    /// If true, a body dying immediately sends its current occupant mind home (cascading if that
    /// home is occupied) instead of waiting for the timer.
    /// </summary>
    [DataField]
    public bool RevertOnDeath = true;

    // ================= Crit phase (Eye tether/possession) =================
    // The crack-open visual/core/pulse-stopping behavior itself is handled generically by the
    // base AnomalyComponent (see SupercriticalAnimationState and friends), not here.

    /// <summary>Range (and line-of-sight requirement) to grab victims from.</summary>
    [DataField(required: true)]
    public float TetherRange = 9f;

    /// <summary>How far a possessed body will be allowed to wander from the anomaly while idle (Y).</summary>
    [DataField(required: true)]
    public float PatrolRadius = 5f;

    /// <summary>How close an outsider (anyone not part of the hivemind) has to get to a possessed
    /// body before it breaks patrol and attacks them.</summary>
    [DataField(required: true)]
    public float AttackRange = 7f;

    /// <summary>How far a possessed body will be allowed to chase an intruder before returning (Z).</summary>
    [DataField(required: true)]
    public float ChaseRange = 10f;

    /// <summary>How long a MindShield holder gets to run before being taken over.</summary>
    [DataField]
    public TimeSpan MindShieldGrace = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Radio message broadcast (see EyeAnomalySystem.TriggerEyeSos) whenever a capture involves a
    /// MindShield holder's grace period expiring and their mind actually getting taken - their
    /// MindShield is destroyed at that same moment (see EyeAnomalySystem.CaptureVictimMind),
    /// which this message reports. {0} = name, {1} = X coordinate, {2} = Y coordinate. Always
    /// used for that capture specifically, regardless of whether this same victim has ever been
    /// tethered before - a later capture with a freshly re-equipped MindShield is just as
    /// legitimately "shielded" an event as the first one.
    /// </summary>
    [DataField]
    public string SosMessageMindShielded = "EMERGENCY: Mindshield destroyed during hostile psychic takeover of {0}. Last known location at ({1:F1}, {2:F1}).";

    /// <summary>
    /// Radio message broadcast for a victim with no MindShield, and also always used for the
    /// manual "Send SOS" button - by the time that's usable, any MindShield they had for this
    /// tether has already been destroyed (see EyeAnomalySystem.CaptureVictimMind), so the button
    /// has no need to check for one at all. {0} = name, {1} = X coordinate, {2} = Y coordinate.
    /// </summary>
    [DataField]
    public string SosMessageUnshielded = "ALERT: unauthorized psychic intrusion detected in {0}. Last known location at ({1:F1}, {2:F1}).";

    /// <summary>
    /// Implant a victim needs (see EyeAnomalySystem.HasJobImplant) for a capture to grant the
    /// "Open Death Network" SOS action and trigger the automatic SOS broadcast at all - gates out
    /// wildlife/animals (e.g. cows) that get tethered/possessed exactly like anyone else, but have
    /// no crew job and thus nobody to plausibly notify. Tether, mind capture, guardian AI, and
    /// hivemind speech are all unaffected either way - this only gates the SOS broadcast/button.
    /// </summary>
    [DataField]
    public EntProtoId JobImplantPrototype = "JobNetworkImplant";

    /// <summary>
    /// Entity prototype spawned by TetherVisualSystem to visually connect this anomaly to each
    /// tethered victim - see EyeTetherVisual for the actual prototype (a continuously-tracking
    /// stretchy sprite, not a periodically-redrawn beam - see TetherVisualSystem for why).
    /// </summary>
    [DataField]
    public EntProtoId TetherVisualPrototype = "EyeTetherVisual";

    /// <summary>
    /// Entity prototype spawned to hold a tethered victim's mind for the duration of the tether -
    /// see EyeMindVesselComponent and EyeAnomalySystem's tether-acquisition/BreakTether logic.
    /// Only spawned for a victim that actually has a mind to capture in the first place.
    /// </summary>
    [DataField]
    public EntProtoId MindVesselPrototype = "EyeMindVessel";

    /// <summary>How long the tether "connect" (reach-out) animation takes.</summary>
    [DataField]
    public float ConnectDuration = 0.15f;

    /// <summary>How long the tether "disconnect" (retract) animation takes.</summary>
    [DataField]
    public float DisconnectDuration = 0.2f;

    /// <summary>Whether cracking open will grant a research bonus, once that logic exists.</summary>
    [DataField]
    public bool GrantResearchOnCrack = true;

    // ================= Death (no thralls left) =================
    // Once every victim tethered to this eye has escaped/died and none remain, the anomaly plays
    // its final animation and then destroys itself, dropping the generic AnomalyComponent's own
    // CorePrototype (SuppressCoreOnSupercritical keeps that same core from being dropped early,
    // on the first supercritical event, so it's still available for this final drop). The
    // animation itself is driven by the generic AnomalyComponent.DeathAnimationState/
    // DeathAnimationDuration (same place as SupercriticalAnimationState/SupercriticalSettledState/
    // SupercriticalPulseState), not by anything on this component. See EyeAnomalySystem.
    // OnSupercriticalSettled (starts dying immediately if the initial tether burst catches nobody
    // at all)/BreakTether (starts dying once every existing thrall is gone)/Update (finishes it
    // once DeathAnimationDuration elapses, or cancels it if a new victim gets tethered
    // mid-animation).

    /// <summary>
    /// Runtime-only: true while the death animation is playing, between BreakTether noticing the
    /// last thrall is gone and Update() actually deleting the entity.
    /// </summary>
    public bool IsDying;

    /// <summary>Runtime-only: when the death animation was started, for timing its duration.</summary>
    public TimeSpan? DyingSince;
}
