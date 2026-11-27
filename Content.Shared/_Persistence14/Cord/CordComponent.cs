
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Persistence14.Cord;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CordComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 StartPosition = Vector2.Zero;
    [DataField, AutoNetworkedField]
    public Vector2 EndPosition = Vector2.Zero;

    /// <summary>
    /// Determines if the sprite should be scaled or tiled to meet end points.
    /// </summary>
    [DataField]
    public CordRenderMode Mode = CordRenderMode.Tile;
    /// <summary>
    /// The sprite for the cord to render.
    /// </summary>
    [DataField]
    public float WidthScale = 1f;
}

[Serializable, NetSerializable]
public enum CordRenderMode
{
    Stretch, // Scales the sprite, stretching to meet end points
    Tile // Tiles the sprite. Make sure sprite is vertically tilable.
}
