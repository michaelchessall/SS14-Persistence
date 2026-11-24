namespace Content.Server.Salvage.Magnet;

/// <summary>
/// Server-only references used to update and clean up the independent tether visual.
/// </summary>
[RegisterComponent]
public sealed partial class SalvageMagnetTetherSourceComponent : Component
{
    public EntityUid Magnet;
    public EntityUid Target;
    public EntityUid Data;
}
