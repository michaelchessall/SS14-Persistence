using Content.Shared.Chat.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Systems;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Persistence14.EntityEffects;

public sealed partial class EmoteFit : EntityEffectBase<EmoteFit>
{
    [DataField(required: true)]
    public ProtoId<AutoEmotePrototype> EmoteId;

    [DataField(required: true)]
    public TimeSpan Duration;

    [DataField]
    public UpdateMode Mode = UpdateMode.Set;

    public enum UpdateMode
    {
        Set, // Sets the current duration to the provided duration.
        Add // Adds the provided duration to the current duration.
    }
}