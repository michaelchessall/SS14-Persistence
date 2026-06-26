using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

/// <summary>
///     Set of required information to draw a stamp in UIs, where
///     representing the state of the stamp at the point in time
///     when it was applied to a paper. These fields mirror the
///     equivalent in the component.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial struct StampDisplayInfo
{
    /// <summary>
    /// Localization name / stamp to be contained in the stamp.
    /// </summary>
    [DataField("stampedName")]
    public string StampedName;

    /// <summary>
    /// Color of the stamp
    /// </summary>

    [DataField("stampedColor")]
    public Color StampedColor;

    /// <summary>
    /// Determines if the stamp should use the border box
    /// </summary>

    [DataField("useBox")]
    public bool UseBox = true;

    /// <summary>
    /// Determines if StampName should be considered a Loc name or raw text.
    /// </summary>
    [DataField]
    public bool UseNameAsLoc = true;
};

[RegisterComponent]
public sealed partial class StampComponent : Component
{
    /// <summary>
    ///     The loc string name that will be stamped to the piece of paper on examine.
    /// </summary>
    [DataField("stampedName")]
    public string StampedName { get; set; } = "stamp-component-stamped-name-default";

    /// <summary>
    ///     The sprite state of the stamp to display on the paper from paper Sprite path.
    /// </summary>
    [DataField("stampState")]
    public string StampState { get; set; } = "paper_stamp-generic";

    /// <summary>
    /// The color of the ink used by the stamp in UIs
    /// </summary>
    [DataField("stampedColor")]
    public Color StampedColor = Color.FromHex("#BB3232"); // StyleNano.DangerousRedFore

    /// <summary>
    /// The sound when stamp stamped
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = null;
}
