using Content.Shared._Persistence14.Resonance.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Set order in database as approved.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationModificationDefaultAccess : BoundUserInterfaceMessage
{

    public StationModificationDefaultAccess()
    {
    }
}

[Serializable, NetSerializable]
public sealed class StationModificationResonancePurchase : BoundUserInterfaceMessage
{
    public ProtoId<ResonanceProductPrototype> Prototype;
    public StationModificationResonancePurchase(ProtoId<ResonanceProductPrototype> prototype)
    {
        Prototype = prototype;
    }
}
