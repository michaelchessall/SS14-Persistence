namespace Content.Shared._Persistence14.Radiation;

[RegisterComponent]
public sealed partial class RadioactiveDecayComponent : Component
{
    /// <summary>
    /// The amount of time it takes for the intensity of the <see cref="Content.Shared.Radiation.Components.RadiationSourceComponent.Intesity"/> to half in value.
    /// Follows exponential decay.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan HalfLife;

    /// <summary>
    /// The threshold at which the RadiationSourceComponent will be removed from the entity.
    /// Establishes the "minimum radioactiveness" to be considered.
    /// </summary>
    [DataField]
    public float MinimumRadiationIntesnity = 0.1f;
}