namespace Content.Server.Atmos;

[RegisterComponent]
public sealed partial class FireproofComponent : Component
{
    /// <summary>
    /// Determines if a container protects its contents from fire.
    /// </summary>
    [DataField]
    public bool ProtectContents = true;

    /// <summary>
    /// Maximum temperature a fireproof item can reach
    /// </summar>
    [DataField]
    public float MaxTemperature = 370; // Just below the ignition tempurature for paper. Creatures will still take damage if in a fireproof container.
}