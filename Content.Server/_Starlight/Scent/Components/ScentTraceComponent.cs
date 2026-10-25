using Content.Server._Starlight.Scent.Systems;
using Content.Server.Speech;

namespace Content.Server._Starlight.Scent.Components;

// Object-side counterpart to ScentComponent, mirroring ForensicsComponent for fingerprints/DNA.
// Server-only. Revealed via the sniff-object DoAfter/BUI.
[RegisterComponent, Access(typeof(ScentSystem))]
public sealed partial class ScentTraceComponent : Component
{
    // ScentId -> trace info. Re-touching refreshes the entry.
    [DataField]
    public Dictionary<string, ScentTraceInfo> Scents = new();

    [DataField]
    public float CleanDistance = 1.5f;

    // How long an entry lingers before naturally expiring, in seconds.
    [DataField]
    public float TraceLifetime = 1800f; // Persistence 14: Increased to 30 minutes.

    /// <summary>
    /// Fraction of TraceLifetime a Partial perceiver judges freshness against instead of the
    /// real value, so they read a trace as stale well before it actually expires.
    /// </summary>
    [DataField]
    public float PartialFreshnessFraction = 1f; // Persistence 14: Increased to 1, partial smellers get same freshness.
}

[DataDefinition]
public partial struct ScentTraceInfo
{
    [DataField]
    public TimeSpan LastTouched;

    // Depositor's species prototype ID, or null with no HumanoidAppearanceComponent. Raw ID.
    // Resolve to a display name via SpeciesPrototype.Name when displaying.
    [DataField]
    public string? Species;
}
