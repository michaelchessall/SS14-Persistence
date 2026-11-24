using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Salvage.Magnet;

/// <summary>
/// Independent world-space endpoints let the navigation beam remain visible outside either endpoint's PVS.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SalvageMagnetTetherComponent : Component
{
    [DataField, AutoNetworkedField]
    public MapCoordinates Start;

    [DataField, AutoNetworkedField]
    public MapCoordinates End;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier Sprite = default!;

    [DataField, AutoNetworkedField]
    public Color Color = Color.White;
}
