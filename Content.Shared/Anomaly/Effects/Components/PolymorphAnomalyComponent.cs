using Content.Shared.Anomaly.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Effects.Components;

/// <summary>
/// Anomaly effect that grabs every valid mob (players and NPCs alike) in range on pulse
/// and forcibly polymorphs them into a random entity, for a random duration.
/// On supercritical, the effect happens again over a larger area, and each victim has a chance
/// for the transformation to become permanent instead of timing out.
/// </summary>
[RegisterComponent]
public sealed partial class PolymorphAnomalyComponent : Component
{
    /// <summary>
    /// The table of possible polymorph outcomes (and their durations) that this anomaly rolls from.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphAnomalyTablePrototype> PolymorphTable;

    /// <summary>
    /// The radius, in tiles, that a normal pulse will affect.
    /// </summary>
    [DataField]
    public float Range = 5f;

    /// <summary>
    /// The radius, in tiles, that a crit event will affect.
    /// </summary>
    [DataField]
    public float SupercriticalRange = 10f;

    /// <summary>
    /// Chance, per victim, that a polymorph rolled during a supercritical event becomes
    /// permanent (i.e. its duration is cleared entirely) instead of using the normal rolled duration.
    /// A permanent polymorph can still be reverted by death or critical condition.
    /// </summary>
    [DataField]
    public float SupercriticalPermanentChance = 0.4f;

    /// <summary>
    /// Sound played at the anomaly whenever it polymorphs something.
    /// </summary>
    [DataField]
    public SoundSpecifier? PolymorphSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
