using Content.Server.Shuttles.Systems;
using Content.Shared.Damage;
using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Numerics;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
    [Access(typeof(ThrusterSystem))]
    public sealed partial class ThrusterComponent : Component
    {
        /// <summary>
        /// If the thruster can be toggled on or off via interaction
        /// </summary>
        [DataField]
        public bool CanToggle = true;

        /// <summary>
        /// Whether the thruster has been force to be enabled / disabled (e.g. VV, interaction, etc.)
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Base power for the <see cref="ApcPowerReceiverComponent"/>, scaled by thruster setting.
        /// </summary>
        [DataField]
        public float BasePowerLoad = 1500;

        /// <summary>
        /// This determines whether the thruster is actually enabled for the purposes of thrust
        /// </summary>
        public bool IsOn;

        // Need to serialize this because RefreshParts isn't called on Init and this will break post-mapinit maps!
        [ViewVariables(VVAccess.ReadWrite), DataField("thrust")]
        public float Thrust = 160000f;

        /// <summary>
        /// Throttles the influence of gyroscopes on small shuttles. The default value is roughly half the inertia of the standard 4-door cargo shuttle.
        /// </summary>
        [DataField]
        public float InertiaThreshold = 850000f;

        [DataField("thrusterType")]
        public ThrusterType Type = ThrusterType.Linear;

        /// HULLROTPORT:
        /// Shape of the exhaust trail, in the thruster's local frame, where +Y points out of the nozzle.
        /// Persistence: defaults to a 1x3 trail so anything sitting behind the nozzle cooks.
        /// Must be convex; a hull is taken over these points.
        ///
        [DataField("burnShape")]
        public List<Vector2> BurnPoly = new()
        {
            new Vector2(-0.45f, 0.5f),
            new Vector2(-0.45f, 3.45f),
            new Vector2(0.45f, 3.45f),
            new Vector2(0.45f, 0.5f)
        };

        /// <summary>
        /// Persistence: whether the trail also burns entities anchored to the thruster's own grid.
        /// Turn this off for ships that are meant to survive their own exhaust.
        /// </summary>
        [DataField]
        public bool BurnOwnGrid = true;

        /// <summary>
        /// How much damage is done per <see cref="FireCooldown"/> to anything inside our exhaust trail.
        /// </summary>
        [DataField("damage")] public DamageSpecifier? Damage = new();

        [DataField("requireSpace")]
        public bool RequireSpace = true;

        // Used for burns

        public bool Firing = false;

        /// <summary>
        /// How often thruster deals damage.
        /// </summary>
        [DataField]
        public TimeSpan FireCooldown = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Next time we tick damage for anyone colliding.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
        public TimeSpan NextFire = TimeSpan.Zero;

        /// <summary>
        /// Togglable thrusters
        /// </summary>
        [DataField]
        public ProtoId<SinkPortPrototype> OnPort = "On";

        [DataField]
        public ProtoId<SinkPortPrototype> OffPort = "Off";

        [DataField]
        public ProtoId<SinkPortPrototype> TogglePort = "Toggle";
    }

    public enum ThrusterType
    {
        Linear,
        // Angular meaning rotational.
        Angular,
    }
}
