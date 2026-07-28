using Content.Shared._Persistence14.PersistentIdentifier.Reference;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Effects.Components;

/// <summary>
/// Lives on a body currently targeted or possessed by an
/// Content.Shared.Anomaly.Effects.Components.EyeAnomalyComponent tether. Tracks everything needed
/// to animate the connection and to fully restore the original player once the tether breaks.
/// </summary>
[RegisterComponent]
public sealed partial class TetheredByEyeComponent : Component
{
    /// <summary>
    /// The eye anomaly this body is tethered to. PersistentEntityReference (not a raw EntityUid)
    /// so the link survives a world save/reload - UIDs are reassigned each session.
    /// </summary>
    [DataField]
    public PersistentEntityReference Eye;

    /// <summary>
    /// The mind that was piloting this body at the moment it was captured. Kept so it can be
    /// handed straight back on disconnect regardless of what happens to it in the meantime.
    /// Resolves to nothing (default EmptyId) during the brief MindShield grace window, since
    /// control hasn't actually been taken yet at that point. PersistentEntityReference for
    /// save/reload stability.
    /// </summary>
    [DataField]
    public PersistentEntityReference Mind;

    /// <summary>
    /// The EyeMindVessel entity the victim's mind was moved into for the duration of the tether -
    /// NOT a normal ghost. It has no way to move at all and lives parented directly to the eye
    /// anomaly, so the trapped player can watch helplessly but cannot act, and anything they say
    /// only becomes audible through the hivemind chorus relay rather than being overheard at its
    /// own (inaccessible) location. Null during the MindShield grace window; deleted on
    /// disconnect. PersistentEntityReference for save/reload stability.
    /// </summary>
    [DataField]
    public PersistentEntityReference MindHost;

    /// <summary>
    /// Current animation/possession phase of this tether.
    /// </summary>
    [DataField]
    public TetherState State = TetherState.Connecting;

    /// <summary>
    /// Progress through the current connect/disconnect animation, 0 to 1. While Connecting this
    /// rises from 0 to 1 over EyeAnomalyComponent.ConnectDuration; while Disconnecting it falls
    /// from 1 to 0 over DisconnectDuration. Once Disconnecting reaches 0, cleanup finishes and
    /// this component is removed.
    /// </summary>
    [DataField]
    public float Progress;

    /// <summary>
    /// Time remaining until the tether beam should be torn down and redrawn. Unused now that
    /// tethering uses TetherVisualSystem's continuously-tracking entity instead of periodically
    /// redrawn beam segments - kept for now in case a future need for periodic redraw returns.
    /// </summary>
    [DataField]
    public float RedrawAccumulator;

    /// <summary>
    /// The TetherVisualComponent-having entity drawing the connection between the eye and this
    /// victim, so it can be cleanly deleted once the tether breaks. PersistentEntityReference for
    /// save/reload stability.
    /// </summary>
    [DataField]
    public PersistentEntityReference VisualEntity;

    /// <summary>
    /// If this victim had a MindShield when targeted, how much longer they keep free control
    /// before it's stripped and full possession begins. Null if they never had one (possession
    /// starts immediately) or if the grace period has already resolved one way or the other.
    /// </summary>
    [DataField]
    public float? MindShieldGraceRemaining;

    /// <summary>
    /// The manually-granted "Open Death Network" action entity (the same one dead players get -
    /// see EyeAnomalySystem's GetSosOverrideEvent handler), letting this ALIVE victim manually
    /// send another SOS after their automatic first one. Tracked separately from
    /// MobStateActionsComponent.GrantedActions (rather than added to that same list) since that
    /// list gets entirely wiped and rebuilt on every MobState change (e.g. entering crit) - this
    /// way a crit/death transition can't accidentally revoke it, and BreakTether can cleanly
    /// remove exactly this one action without touching whatever the stock system granted.
    /// </summary>
    [DataField]
    public EntityUid? SosActionEntity;

    /// <summary>
    /// The victim's own NPC factions at the moment of possession, saved so they can be restored
    /// exactly once the tether breaks - same save/restore pattern as the stock cursed mask
    /// takeover (see Content.Server.Clothing.Systems.CursedMaskSystem). While possessed, the body
    /// is switched entirely to the EyeThrall faction (see ai_factions.yml) instead.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> OldFactions = new();
}

public enum TetherState : byte
{
    /// <summary>MindShield holder still has free control; beam is attached but nothing else is.</summary>
    Grace,
    Connecting,
    Connected,
    Disconnecting,
}
