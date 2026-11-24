using Content.Shared._Persistence14.Cord;
using Content.Shared._Persistence14.PersistentIdentifier.Reference;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage.Magnet;

[RegisterComponent]
public sealed partial class SalvageMagnetComponent : Component
{
    /// <summary>
    /// The max distance at which the magnet will pull in wrecks.
    /// Scales from 50% to 100%.
    /// </summary>
    [DataField]
    public float MagnetSpawnDistance = 64f;

    /// <summary>
    /// How far offset to either side will the magnet wreck spawn.
    /// </summary>
    [DataField]
    public float LateralOffset = 16f;

    /// <summary>
    /// The tractor beam to be spawned connected to the magent and any spawned grid.
    /// </summary>
    [DataField]
    public EntProtoId<CordComponent> TractorCordProto = default!;
}
