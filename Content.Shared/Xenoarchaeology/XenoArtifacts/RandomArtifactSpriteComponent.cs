using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomArtifactSpriteComponent : Component
{
    [DataField, AutoNetworkedField]
    public int SelectedSprite = -1;

    [DataField("minSprite")]
    public int MinSprite = 1;

    [DataField("maxSprite")]
    public int MaxSprite = 14;

    [DataField("activationTime")]
    public double ActivationTime = 0.4;

    public TimeSpan? ActivationStart;
}
