using Content.Server._Persistence14.DeviceLinking.Systems;

namespace Content.Server._Persistence14.DeviceLinking.Components;

/// <summary>
/// When this item is placed inside a plushie (or any squeezable toy with emit-sound components),
/// the plushie gains device-link sink ports — one per distinct sound the plushie can play — so that
/// buttons and other signal sources can be wired up to play those sounds. Removing the activator (or
/// moving it to a different plushie) tears the links down again.
/// </summary>
[RegisterComponent, Access(typeof(PlushieSoundActivatorSystem))]
public sealed partial class PlushieSoundActivatorComponent : Component
{
    /// <summary>
    /// Maximum number of distinct sounds that can be exposed as sink ports.
    /// This should not exceed the number of PlushieSound* sink port prototypes that exist.
    /// </summary>
    [DataField]
    public int MaxSounds = 10;

    /// <summary>
    /// The plushie this activator is currently wired into, if any at all.
    /// </summary>
    [ViewVariables]
    public EntityUid? LinkedPlushie;
}
