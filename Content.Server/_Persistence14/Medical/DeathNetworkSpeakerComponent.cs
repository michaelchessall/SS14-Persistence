namespace Content.Server._Persistence14.Medical;

/// <summary>
/// This is used by the death network when searching for an existing entity
/// to use as the source of the SOS broadcast message.
///
/// If an entity with this component is not found, one is created from the
/// prototype with ID DeathNetworkSpeaker.
/// </summary>
[RegisterComponent]
public sealed partial class DeathNetworkSpeakerComponent : Component;
