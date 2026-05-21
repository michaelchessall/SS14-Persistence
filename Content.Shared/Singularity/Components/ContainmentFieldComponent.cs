using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainmentFieldComponent : Component
{
    /// <summary>
    /// How long a field can remain disconnected before being removed.
    /// </summary>
    [DataField]
    public TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The throw force for the field if an entity collides with it
    /// The lighter the mass the further it will throw. 5 mass will go about 4 tiles out, 70 mass goes only a couple tiles.
    /// </summary>
    [DataField("throwForce")]
    public float ThrowForce = 100f;

    /// <summary>
    /// This shouldn't be at 99999 or higher to prevent the singulo glitching out
    /// Will throw anything at the supplied mass or less that collides with the field.
    /// </summary>
    [DataField("maxMass")]
    public float MaxMass = 10000f;

    /// <summary>
    /// Should field vaporize garbage that collides with it?
    /// </summary>
    [DataField]
    public bool DestroyGarbage = true;

    /// <summary>
    /// The entity UID of the generator that created this field.
    /// Used to rebuild connections after a persistence load.
    /// </summary>
    [DataField]
    public EntityUid? GeneratorUid;

    /// <summary>
    /// The direction this field is oriented.
    /// North/South = Direction North, East/West = Direction East
    /// </summary>
    [DataField]
    public Direction FieldDirection = Direction.North;
}

[Serializable, NetSerializable]
public enum ContainmentFieldVisuals : byte
{
    BreachResistant,
}
