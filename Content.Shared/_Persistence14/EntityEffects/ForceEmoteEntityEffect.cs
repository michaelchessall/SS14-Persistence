using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.EntityEffects;

public sealed partial class ForceEmote : EntityEffectBase<ForceEmote>
{
    /// <summary>
    /// The ID of the emote prototype.
    /// </summary>
    [DataField("emote", required: true)]
    public ProtoId<EmotePrototype> EmoteId = string.Empty;

    /// <summary>
    /// Also send the emote in chat.
    /// </summary>
    [DataField]
    public bool WithChat = true;

    /// <summary>
    /// Should we ignore action blockers?
    /// This does nothing if WithChat is false.
    /// </summary>
    [DataField]
    public bool IgnoreActionBlocker;

    /// <summary>
    /// Should we ignore whitelists and force the emote?
    /// This does nothing if WithChat is false.
    /// </summary>
    [DataField]
    public bool Force;

    /// <summary>
    /// Hide the chat message from the chat window, only showing the popup.
    /// This does nothing if WithChat is false.
    /// </summary>
    [DataField]
    public bool HiddenFromChatWindow;
}

public sealed partial class ForceEmoteEntityEffectSystem : EntityEffectSystem<TransformComponent, ForceEmote>
{
    [Dependency] private SharedChatSystem _chat = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ForceEmote> args)
    {
        if (args.Effect.WithChat)
            _chat.TryEmoteWithChat(entity.Owner, args.Effect.EmoteId,
                ignoreActionBlocker: args.Effect.IgnoreActionBlocker,
                forceEmote: args.Effect.Force,
                hideLog: args.Effect.HiddenFromChatWindow);
        else
            _chat.TryEmoteWithoutChat(entity.Owner, args.Effect.EmoteId);
    }
}