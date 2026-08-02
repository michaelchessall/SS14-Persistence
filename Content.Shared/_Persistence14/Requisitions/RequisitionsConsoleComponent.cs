using System;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.Requisitions;

/// <summary>
/// A customer-facing ordering console. It links to nearby item-printing machines (lathes) the way an ore silo
/// links to its clients, gathers their combined recipe list, and prints a whole cart in one checkout. Payment,
/// when charged, is a printed invoice paid via bank into the console's owning faction. An access-gated tab lets
/// an operator set each raw material's price and define extra fees.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedRequisitionsConsoleSystem))]
public sealed partial class RequisitionsConsoleComponent : Component
{
    #region Linking

    /// <summary>
    /// The item-printing machines (things with <see cref="LatheComponent"/>) this console dispatches prints to.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> LinkedMachines = new();

    /// <summary>
    /// The maximum distance a machine can be from the console and still be linkable. Mirrors the ore silo.
    /// </summary>
    [DataField]
    public float Range = 10f;

    #endregion

    #region Pricing configuration (server-authoritative, pushed to clients via BUI state)

    /// <summary>
    /// Operator-set price charged per unit of a given raw material. Only materials that appear in the linked
    /// machines' recipes are ever shown or priced.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> MaterialPrices = new();

    /// <summary>Extra named charges (research fee, handling fee, the automatic flatpack fee, …).</summary>
    [DataField]
    public List<RequisitionFee> Fees = new();

    /// <summary>Default per-material prices seeded from YAML when a material first becomes priceable.</summary>
    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> DefaultMaterialPrices = new();

    /// <summary>Fallback price for a priceable material not listed in <see cref="DefaultMaterialPrices"/>.</summary>
    [DataField]
    public int FallbackMaterialPrice;

    /// <summary>
    /// When true, a printed invoice itemises each line's materials and fees plus per-order totals. When false,
    /// the invoice is trimmed to just one line per item ("name — cost"), failures, and the grand total.
    /// </summary>
    [DataField]
    public bool DetailedInvoice = true;

    #endregion

    #region State

    /// <summary>
    /// Number of requisition prints still in progress across the linked machines. While &gt; 0 the console is
    /// "processing a checkout" and refuses to start another one.
    /// </summary>
    [DataField]
    public int OutstandingJobs;

    #endregion

    #region Flatpack storage

    /// <summary>
    /// Internal container holding printed boards waiting to be flatpacked. Boards are moved here (not held in
    /// memory) so nothing is lost if packing stalls; an authorised operator can eject them from the config tab.
    /// </summary>
    [DataField]
    public string FlatpackStorageId = "requisitions-flatpack-storage";

    /// <summary>Earliest time to retry feeding a flatpacker, so a stalled pack doesn't churn every tick.</summary>
    [DataField]
    public TimeSpan NextFlatpackTry;

    #endregion

    #region Flatpack

    /// <summary>
    /// Set true when at least one linked machine is a flatpack creator. Enables the flatpack column and fee.
    /// </summary>
    [DataField]
    public bool FlatpackerLinked;

    /// <summary>
    /// The id of the automatic flatpack fee entry in <see cref="Fees"/>.
    /// </summary>
    [DataField]
    public string FlatpackFeeId = "Flatpack";

    /// <summary>
    /// Multiplier applied to a recipe's material cost when it is flatpacked. Flatpacking is more expensive.
    /// </summary>
    [DataField]
    public float FlatpackMaterialMultiplier = 1.5f;

    #endregion
}

/// <summary>
/// An extra charge the operator can attach to some or all catalogue items.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class RequisitionFee
{
    /// <summary>Stable identifier for this fee (used by config messages and the flatpack fee).</summary>
    [DataField(required: true)]
    public string Id = default!;

    /// <summary>Player-facing name, e.g. "Research Fee".</summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// For a <see cref="RequisitionFeeType.Flat"/> fee, the flat charge in the console's currency. For a
    /// <see cref="RequisitionFeeType.Percent"/> fee, the percentage added to the item's material value.
    /// </summary>
    [DataField]
    public int Price;

    /// <summary>Whether <see cref="Price"/> is a flat charge or a percentage of the item's value.</summary>
    [DataField]
    public RequisitionFeeType Type = RequisitionFeeType.Flat;

    /// <summary>Which catalogue items this fee applies to.</summary>
    [DataField]
    public RequisitionFeeScope Scope = RequisitionFeeScope.Specific;

    /// <summary>When <see cref="Scope"/> is <see cref="RequisitionFeeScope.Specific"/>, the recipes it applies to.</summary>
    [DataField]
    public HashSet<ProtoId<LatheRecipePrototype>> Recipes = new();

    /// <summary>
    /// How much this fee adds to a single cart line, given that line's material value (<paramref name="itemWorth"/>)
    /// and quantity. Percent fees scale with the value; flat fees are charged per unit.
    /// </summary>
    public int AmountFor(int itemWorth, int quantity)
    {
        return Type == RequisitionFeeType.Percent
            ? (int) MathF.Round(itemWorth * Price / 100f)
            : Price * quantity;
    }
}

[Serializable, NetSerializable]
public enum RequisitionFeeType : byte
{
    /// <summary>A fixed charge, per unit.</summary>
    Flat,

    /// <summary>A percentage added on top of the item's material value.</summary>
    Percent,
}

[Serializable, NetSerializable]
public enum RequisitionFeeScope : byte
{
    /// <summary>Only the recipes listed in <see cref="RequisitionFee.Recipes"/>.</summary>
    Specific,

    /// <summary>Every catalogue item.</summary>
    All,

    /// <summary>Only items that are being flatpacked. Reserved for the automatic flatpack fee.</summary>
    Flatpack,
}
