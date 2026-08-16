using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when a mob performs an emote nearby
/// (e.g. laughing, sighing, sneezing).
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATEmoteSystem)), AutoGenerateComponentState]
public sealed partial class XATEmoteComponent : Component
{
    /// <summary>
    /// Range within which the artifact listens for emotes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 5;

    /// <summary>
    /// Emotes that can activate this trigger. If empty, any emote works.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<EmotePrototype>> Emotes = new();
}
