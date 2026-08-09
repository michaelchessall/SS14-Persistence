using Content.Shared.FixedPoint;
using Content.Shared.Fluids.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids.Components;

[RegisterComponent]
[Access(typeof(SharedSpraySystem))]
public sealed partial class SprayComponent : Component
{
    [DataField]
    public string Solution = "spray";

    [DataField]
    public FixedPoint2 TransferAmount = 10;

    /// <summary>
    /// If true, the amount sprayed per use is taken from this entity's <c>SolutionTransfer</c> component
    /// (letting the player pick it via the "set transfer amount" context verb) instead of the fixed
    /// <see cref="TransferAmount"/> above. Has no effect if the entity has no SolutionTransfer component.
    /// </summary>
    [DataField]
    public bool UseSolutionTransferAmount;

    [DataField]
    public float SprayDistance = 3.5f;

    [DataField]
    public float SprayVelocity = 3.5f;

    [DataField]
    public EntProtoId SprayedPrototype = "Vapor";

    [DataField]
    public int VaporAmount = 1;

    [DataField]
    public float VaporSpread = 90f;

    /// <summary>
    /// How much the player is pushed back for each spray.
    /// </summary>
    [DataField]
    public float PushbackAmount = 5f;

    [DataField(required: true)]
    [Access(typeof(SharedSpraySystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public SoundSpecifier SpraySound { get; private set; } = default!;

    [DataField]
    public LocId SprayEmptyPopupMessage = "spray-component-is-empty-message";
}
