namespace Content.Shared.Mobs.Events;

/// <summary>
/// Raised on an entity when checking whether they can use the "Send SOS" action (see
/// Content.Server.Mobs.CritMobActionsSystem) and again right before actually sending it, letting
/// any system grant SOS access to something that isn't MobState.Dead and/or override the
/// message text that gets sent - e.g. the Eye anomaly's tethered-victim hivemind, where a
/// captured mind is very much still alive
/// </summary>
public sealed class GetSosOverrideEvent : EntityEventArgs
{
    /// <summary>If set to true by any subscriber, allows SOS even though the entity isn't MobState.Dead.</summary>
    public bool AllowWhileAlive;

    /// <summary>
    /// If set (non-null) by any subscriber, this message is sent instead of the default
    /// "has died at ..." one. Whoever sets this is responsible for formatting it completely
    /// (name, coordinates, etc.) - it's used verbatim.
    /// </summary>
    public string? MessageOverride;

    /// <summary>
    /// If set (non-null) by any subscriber, this entity is used as the radio message's source
    /// instead of the entity the action is actually attached to - separate from MessageOverride,
    /// since the radio wrapper that shows WHO sent the message (via TransformSpeakerNameEvent,
    /// which reads straight off this entity's own name) is computed entirely independently of
    /// the message text itself. For something like a mind vessel, whose own name/identity makes
    /// no sense to show as the sender, this redirects that label to the original body instead.
    /// </summary>
    public EntityUid? SpeakerOverride;
}
