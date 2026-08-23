using Content.Shared.Chat;
using Content.Shared.Mobs.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for a xeno artifact trigger that requires a mob to perform an emote near the artifact.
/// </summary>
public sealed class XATEmoteSystem : BaseXATSystem<XATEmoteComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _xenoArtifactQuery = GetEntityQuery<XenoArtifactComponent>();

        // EmoteEvent is raised (directed) on the emoting entity, not on the artifact, so we can't use
        // the artifact event-relay here. Every mob has MobStateComponent, so a directed subscription
        // on it catches any mob's emote and hands us the emoter directly.
        SubscribeLocalEvent<MobStateComponent, EmoteEvent>(OnEmote);
    }

    private void OnEmote(Entity<MobStateComponent> ent, ref EmoteEvent args)
    {
        var emoterCoords = Transform(ent).Coordinates;

        var query = EntityQueryEnumerator<XATEmoteComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            // An empty Emotes set means any emote triggers it; otherwise only the listed emotes count.
            if (comp.Emotes.Count > 0 && !comp.Emotes.Contains(args.Emote.ID))
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoords = Transform(artifact).Coordinates;
            if (_transform.InRange(emoterCoords, artifactCoords, comp.Range))
                Trigger(artifact, (uid, comp, node));
        }
    }
}
