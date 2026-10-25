using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Power.SMES;

/// <summary>
///     Handles the "user-facing" side of the actual SMES object.
///     This is operations that are specific to the SMES, like UI and visuals.
///     Logic is handled in <see cref="SmesSystem"/>
///     Code interfacing with the powernet is handled in <see cref="BatteryStorageComponent"/> and <see cref="BatteryDischargerComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(SmesSystem))]
public sealed partial class SmesComponent : Component
{
    [ViewVariables]
    public ChargeState LastChargeState;
    [ViewVariables]
    public TimeSpan LastChargeStateTime;
    [ViewVariables]
    public int LastChargeLevel;
    [ViewVariables]
    public TimeSpan LastChargeLevelTime;
    [ViewVariables]
    public TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The number of distinct charge levels a SMES has.
    /// 0 is empty max is full.
    /// </summary>
    [DataField("numChargeLevels")]
    public int NumChargeLevels = 6;

    /// <summary>
    /// The charge level of the SMES as of the most recent update.
    /// </summary>
    [ViewVariables]
    public int ChargeLevel = 0;

    /// <summary>
    /// The port that gets set to high when the battery is at chargelevel0.
    /// </summary>
    [DataField("chargeLevel0Port", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string ChargeLevel0Port = "PowerChargeLevel0";

    /// <summary>
    /// The port that gets set to high when the battery is at chargelevel1.
    /// </summary>
    [DataField("chargeLevel1Port", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string ChargeLevel1Port = "PowerChargeLevel1";

    /// <summary>
    /// The port that gets set to high when the battery is at chargelevel2.
    /// </summary>
    [DataField("chargeLevel2Port", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string ChargeLevel2Port = "PowerChargeLevel2";

     /// <summary>
    /// The port that gets set to high when the battery is at chargelevel3.
    /// </summary>
    [DataField("chargeLevel3Port", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string ChargeLevel3Port = "PowerChargeLevel3";

    /// <summary>
    /// The port that gets set to high when the battery is at chargelevel4.
    /// </summary>
    [DataField("chargeLevel4Port", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string ChargeLevel4Port = "PowerChargeLevel4";

    /// <summary>
    /// The port that gets set to high when the battery is at chargelevel5.
    /// </summary>
    [DataField("chargeLevel5Port", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string ChargeLevel5Port = "PowerChargeLevel5";

    /// <summary>
    /// Whether the SMES is being charged/discharged/neither.
    /// </summary>
    [ViewVariables]
    public ChargeState ChargeState = ChargeState.Still;
}
