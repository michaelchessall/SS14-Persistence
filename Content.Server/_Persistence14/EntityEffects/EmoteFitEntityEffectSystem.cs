using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared._Persistence14.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Systems;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Server._Persistence14.EntityEffects;

public sealed partial class EmoteFitEntityEffectSystem : EntityEffectSystem<TransformComponent, EmoteFit>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private AutoEmoteSystem _autoEmote = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<EmoteFit> args)
    {
        // Only living mobs may emote.
        if (!_mobState.IsAlive(entity.Owner))
            return;

        var fit = EnsureComp<ForcedEmoteFitComponent>(entity.Owner);
        var emote = EnsureComp<AutoEmoteComponent>(entity.Owner);
        _autoEmote.AddEmote(entity.Owner, args.Effect.EmoteId, emote);

        var curr = fit.EndTimes.GetValueOrDefault(args.Effect.EmoteId);
        var newTime = args.Effect.Mode switch
        {
            EmoteFit.UpdateMode.Set => _timing.CurTime + args.Effect.Duration,
            EmoteFit.UpdateMode.Add => curr + args.Effect.Duration,
            _ => curr
        };

        fit.EndTimes[args.Effect.EmoteId] = newTime;
    }
}