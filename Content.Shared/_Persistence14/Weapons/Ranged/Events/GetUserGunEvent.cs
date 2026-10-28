namespace Content.Shared._Persistence14.Weapons.Ranged.Events;

/// <summary>
/// Raised on a user when looking up which held gun they shoot with.
/// Handlers may set <see cref="Gun"/> to another held gun to redirect the shot.
/// </summary>
[ByRefEvent]
public record struct GetUserGunEvent(EntityUid Gun);
