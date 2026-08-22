using Content.Server._Persistence14.DeviceLinking.Systems;
using Robust.Shared.Audio;

namespace Content.Server._Persistence14.DeviceLinking.Components;

/// <summary>
/// Added to a plushie while a <see cref="PlushieSoundActivatorComponent"/> is inside it.
/// Maps each generated sink port to the sound that port should play when it receives a signal.
/// Removed again when the activator leaves the plushie, which is what causes the links to be lost.
/// </summary>
[RegisterComponent, Access(typeof(PlushieSoundActivatorSystem))]
public sealed partial class PlushieSoundLinkComponent : Component
{
    /// <summary>
    /// The activator responsible for these ports and links.
    /// </summary>
    [ViewVariables]
    public EntityUid Activator;

    /// <summary>
    /// Maps sink port id -> the sound to play when that port receives a signal.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, SoundSpecifier> PortSounds = new();
}
