using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger, which gets activated from some gas being on the same time as artifact with certain concentration.
/// </summary>
public sealed class XATGasSystem : BaseQueryUpdateXATSystem<XATGasComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATGasComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        var xform = Transform(artifact);

        if (_atmosphere.GetTileMixture((artifact, xform)) is not { } mixture)
            return;

        var gasTrigger = node.Comp1;
        if (gasTrigger.Gases.Count == 0)
            return;

        if (gasTrigger.ShouldBePresent)
        {
            foreach (var gas in gasTrigger.Gases)
            {
                if (mixture.GetMoles(gas) >= gasTrigger.Moles)
                {
                    Trigger(artifact, node);
                    return;
                }
            }
        }
        else
        {
            foreach (var gas in gasTrigger.Gases)
            {
                if (mixture.GetMoles(gas) > gasTrigger.Moles)
                    return;
            }

            Trigger(artifact, node);
        }
    }
}
