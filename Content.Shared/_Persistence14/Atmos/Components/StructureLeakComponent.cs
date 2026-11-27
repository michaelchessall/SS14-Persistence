using Robust.Shared.GameStates;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared._Persistence14.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class StructureLeakComponent : Component
{
    [AutoNetworkedField]
    public bool Leaking;

    /// <summary>
    /// ID of the <see cref="LocalizedDatasetPrototype"/> containing messages to display if the structure is leaking or not.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Messages;
    [DataField]
    public float LeakThreshold { get; set; } = 0.6f;

    /// <summary>
    /// The sound played when this airtight entity is broken and no longer blocks air.
    /// </summary>
    [DataField]
    public SoundSpecifier BreakSound = new SoundCollectionSpecifier("MetalBreak");
}
