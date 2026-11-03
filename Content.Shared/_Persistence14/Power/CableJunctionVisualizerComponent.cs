namespace Content.Shared._Persistence14.Power;

[RegisterComponent]
public sealed partial class CableJunctionVisualizerComponent : Component
{
    [DataField(required: true)]
    public string Prefix;

    [DataField]
    public string? StripePrefix;

    [DataField]
    public Dictionary<CableJunctionVisualizerVariants, string> Directions = new();

    public string GetRsiState(CableJunctionVisualizerVariants variant) => string.Concat(Prefix, Directions[variant]);
    public bool TryGetStripeRsiState(CableJunctionVisualizerVariants variant, out string state)
    {
        state = "";
        if (StripePrefix is null)
            return false;

        state = string.Concat(StripePrefix, Directions[variant]);
        return true;
    }
}

public enum CableJunctionVisualizerVariants
{
    Center,
    North,
    South,
    East,
    West,
    Vertical,
    Horizontal,
    None,
}

public enum CableJunctionVisualLayers
{
    Horizontal,
    HorizontalStripe,
    Vertical,
    VerticalStripe,
    Junction
}