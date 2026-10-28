namespace Content.Server._Persistence14.GameTicking;

/// <summary>
///     Called once at the start of the round after all expected entities have been loaded and initialized.
/// </summary>
[ByRefEvent]
public readonly record struct PostInitEvent;