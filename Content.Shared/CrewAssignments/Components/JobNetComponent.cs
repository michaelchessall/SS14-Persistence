using Content.Shared.Cargo;
using Content.Shared.CrewAssignments.Prototypes;
using Content.Shared.Precursor;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CrewAssignments.Components;


[Serializable, NetSerializable]
public enum RogueNetworkType : byte
{
    None,
    Dealer,
    BountyHunter,
    Assassin
}


[RegisterComponent, NetworkedComponent]
public sealed partial class JobNetComponent : Component
{
    [DataField]
    public SoundSpecifier PaySuccessSound = new SoundPathSpecifier("/Audio/Effects/kaching.ogg");
    [DataField]
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");
    [DataField]
    public int? WorkingFor;
    [DataField]
    public TimeSpan WorkedTime = TimeSpan.Zero;
    [DataField]
    public int LastWorkedFor = 0;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan PrecursorResetTime = TimeSpan.Zero;
    [DataField]
    public List<ProtoId<PrecursorObjectivePrototype>> PrecursorObjectives = new List<ProtoId<PrecursorObjectivePrototype>>();
    [DataField]
    public int Precursor = 0;
    [DataField]
    public ProtoId<RogueLevelPrototype> RogueLevel = "RogueLevel1";
    [DataField]
    public int XP = 0;
    [DataField]
    public RogueNetworkType NetworkType = RogueNetworkType.None;
    [DataField]
    public string? KillTarget = null;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RogueNetResetTime = TimeSpan.Zero;

    [DataField]
    public string SecretPhrase = "";
    [DataField]
    public CargoBountyData? DealerBounty = null;

    [DataField("nextPrintTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPrintTime = TimeSpan.Zero;
}

