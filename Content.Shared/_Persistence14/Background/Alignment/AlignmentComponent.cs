using Robust.Shared.GameStates;

namespace Content.Shared._Persistence14.Background.Alignment;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlignmentComponent : Component
{
    [DataField, AutoNetworkedField]
    public AlignmentGoodnessStates Goodness { get; set; } = AlignmentGoodnessStates.Neutral;
    [DataField, AutoNetworkedField]
    public AlignmentLawfulnessStates Lawfulness { get; set; } = AlignmentLawfulnessStates.Neutral;
}

public enum AlignmentGoodnessStates
{
    Good,
    Neutral,
    Evil
}

public enum AlignmentLawfulnessStates
{
    Lawful,
    Neutral,
    Chaotic
}
