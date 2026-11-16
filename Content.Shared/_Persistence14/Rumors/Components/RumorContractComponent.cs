using Content.Shared._Persistence14.Rumors.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Rumors.Components;

/// <summary>
/// Marks a Rumor Contract which, when used, gives the user a contract of a particular 
/// </summary>
[RegisterComponent]
public sealed partial class RumorContractComponent : Component
{
    [DataField(required: true)]
    public ProtoId<MetaFactionPrototype> Faction;

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg");
}