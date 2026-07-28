using Content.Shared._Persistence14.PersistentIdentifier.Reference;

namespace Content.Server.Anomaly.Effects.Components;

/// <summary>
/// Marker on the vessel entity a tethered victim's mind gets moved into while trapped inside an
/// Eye anomaly - NOT a ghost, and not free to move or act at all; it's simply co-located with
/// the eye with no way to move, interact, or perceive anything (a deliberately minimal entity -
/// see the EyeMindVessel prototype). When this vessel's occupant speaks (see
/// EyeAnomalySystem.OnVesselSpoke), that speech is relayed as if every body CURRENTLY tethered
/// to the same eye said it simultaneously - a hivemind chorus. The eye anomaly itself never
/// speaks on its own. The mind gets transferred back to whichever body it came from
/// (TetheredByEyeComponent.Mind/OriginalBody on that body) the moment the tether breaks - death,
/// crit, or going out of range, see EyeAnomalySystem.BreakTether - and this vessel is deleted at
/// that point.
///
/// Deliberately not declared as a component in any entity prototype - like
/// TetheredByEyeComponent, it's only ever added programmatically once a mind is actually
/// captured, with Eye/OriginalBody set at that point. Both fields are therefore plain (not
/// required:true) - see the EntProtoId/required:true pitfall noted elsewhere in this codebase
/// for why a field only ever set in code must never be marked required in its DataField.
/// </summary>
[RegisterComponent]
public sealed partial class EyeMindVesselComponent : Component
{
    /// <summary>The eye anomaly this vessel belongs to. PersistentEntityReference so the link
    /// survives a world save/reload (raw EntityUids are reassigned each session).</summary>
    [DataField]
    public PersistentEntityReference Eye;

    /// <summary>The body this vessel's occupant will be returned to once the tether breaks.
    /// PersistentEntityReference for save/reload stability.</summary>
    [DataField]
    public PersistentEntityReference OriginalBody;
}
