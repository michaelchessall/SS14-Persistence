using Content.Shared.Construction.Components;

namespace Content.Shared._Persistence14.Construction;

[RegisterComponent]
public sealed partial class AnchorChangeRequiresAccessComponent : Component
{
    [DataField]
    public AnchorableFlags Flags = AnchorableFlags.Anchorable | AnchorableFlags.Unanchorable;

    [DataField]
    public string FailureMessage = "Access Denied";
}