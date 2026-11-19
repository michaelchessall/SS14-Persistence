using Content.Shared._Persistence14.Storage.EntitySystems;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Persistence14.Storage.Components;

/// <summary>
/// Adds a hidden secondary container underneath an <see cref="Content.Shared.Storage.Components.EntityStorageComponent"/>
/// that can be pried open and secured again with a tool (e.g. a crowbar).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(FalseBottomSystem))]
public sealed partial class FalseBottomComponent : Component
{
    /// <summary>
    /// Whether the false bottom is currently pried open.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Open;

    /// <summary>
    /// Maximum number of items that can be hidden under the false bottom.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Capacity = 10;

    /// <summary>
    /// Maximum width or height of an item allowed under the false bottom.
    /// </summary>
    [DataField]
    public float MaxSize = 0.7f;

    /// <summary>
    /// Tool quality required to pry open or secure the false bottom.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> ToolQuality = "Prying";

    /// <summary>
    /// Base DoAfter duration (in seconds) to pry open or secure the false bottom.
    /// </summary>
    [DataField]
    public float PryTime = 1.5f;

    /// <summary>
    /// Whitelist for entities allowed inside the false bottom.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = ["Item"],
    };

    /// <summary>
    /// Blacklist for entities disallowed inside the false bottom.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Container storing the hidden items.
    /// </summary>
    [ViewVariables]
    public Container HiddenContents = default!;
}