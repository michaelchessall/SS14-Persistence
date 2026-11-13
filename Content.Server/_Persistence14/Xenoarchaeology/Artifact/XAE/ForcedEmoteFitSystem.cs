using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Server._Persistence14.Xenoarcheaology.Artifact.XAE;

public sealed partial class ForcedEmoteFitSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AutoEmoteSystem _autoEmote = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ForcedEmoteFitComponent>();
        while (query.MoveNext(out var uid, out var fit))
        {
            // Snapshot the keys so we can mutate the dictionary while iterating.
            foreach (var autoEmote in fit.EndTimes.Keys.ToArray())
            {
                if (now < fit.EndTimes[autoEmote])
                    continue;

                // removeEmpty (default true) also drops the AutoEmoteComponent once no auto-emotes
                // remain, unless the mob has other auto-emotes of its own (e.g. a cluwne), which are
                // left untouched.
                _autoEmote.RemoveEmote(uid, autoEmote);
                fit.EndTimes.Remove(autoEmote);
            }

            if (fit.EndTimes.Count == 0)
                RemCompDeferred<ForcedEmoteFitComponent>(uid);
        }
    }
}