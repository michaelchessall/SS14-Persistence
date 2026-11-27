namespace Content.Shared._Persistence14.Shuttles;

[RegisterComponent]
public sealed partial class IFFVisualizerComponent : Component
{
    [DataField]
    public string[] Layers = [];

    [DataField]
    public int MaxIffSearchDepth = 1;
}
