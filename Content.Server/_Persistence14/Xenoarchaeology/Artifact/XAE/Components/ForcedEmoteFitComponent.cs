using Content.Server.Xenoarchaeology.Artifact.XAE;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Tracks an ongoing forced-emote "fit" applied by <see cref="XAEForcedEmoteComponent"/>. Holds the
/// AutoEmote to stop and when the fit ends, at which point both are removed as they are pointless.
/// </summary>
[RegisterComponent, Access(typeof(XAEForcedEmoteSystem))]
public sealed partial class ForcedEmoteFitComponent : Component
{
    /// <summary>
    /// The auto-emote to remove when the fit ends.
    /// </summary>
    [DataField]
    public ProtoId<AutoEmotePrototype> AutoEmote;

    /// <summary>
    /// When the fit ends and the mob stops emoting.
    /// </summary>
    [DataField]
    public TimeSpan EndTime;
}
