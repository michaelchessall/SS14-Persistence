using Content.Shared._Funkystation.Stains.Components;
using Content.Shared._Funkystation.Stains.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;

namespace Content.Server._Funkystation.Stains;

public sealed class StainSystem : SharedStainSystem
{
    [Dependency] private readonly TagSystem _tag = null!;

    public const string DNASolutionScannableTag = "DNASolutionScannable";

    protected override void OnStained(Entity<StainableComponent> ent, Entity<SolutionComponent> solution)
    {
        base.OnStained(ent, solution);

        _tag.AddTag(ent.Owner, DNASolutionScannableTag);
    }
}
