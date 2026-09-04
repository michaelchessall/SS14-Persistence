using Content.Shared._Persistence14.Radiation;
using Content.Shared.Radiation.Components;

namespace Content.Server.Radiation.Systems;

public sealed partial class RadiationSystem
{
    private void IncreaseSourceIntensity(Entity<RadiationActivationComponent?> activation, float rads, float frameTime)
    {
        if (!Resolve(activation, ref activation.Comp))
            return;

        var increase = CalculateIntensityIncrease((activation, activation.Comp), rads, frameTime);

        if (increase <= 0f)
            return;


        if (!TryComp<RadiationSourceComponent>(activation, out var radiationSource))
        {
            if (TryComp<RadioactiveDecayComponent>(activation.Owner, out var decay) && increase < decay.MinimumRadiationIntesnity)
                return;

            radiationSource = AddComp<RadiationSourceComponent>(activation.Owner);
            radiationSource.Intensity = increase;
        }
        else
        {
            if (TryComp<RadioactiveDecayComponent>(activation.Owner, out var decay) && radiationSource.Intensity + increase < decay.MinimumRadiationIntesnity)
                return;
            radiationSource.Intensity += increase;
        }
        if (radiationSource.Intensity > activation.Comp.MaxIntensity)
            radiationSource.Intensity = activation.Comp.MaxIntensity;
        Dirty(activation.Owner, radiationSource);
    }

    private float CalculateIntensityIncrease(Entity<RadiationActivationComponent> activation, float rads, float frameTime)
    {
        return rads * activation.Comp.ActivationRate * frameTime;
    }
}