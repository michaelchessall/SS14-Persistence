

namespace Content.Shared.Persistence.Paper;

[RegisterComponent]
public sealed partial class SignatureWriterComponent : Component
{
    /// <summary>
    /// The color of the signature. If null, uses CrayonComponent or black.
    /// </summary>
    [DataField]
    public Color? SignatureColor = null;
}