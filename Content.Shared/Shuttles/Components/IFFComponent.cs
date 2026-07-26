using Content.Shared.Shuttles.Systems;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Handles what a grid should look like on radar.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedShuttleSystem))]
public sealed partial class IFFComponent : Component
{
    public static readonly Color SelfColor = Color.MediumSpringGreen;

    /// <summary>
    /// Default color to use for IFF if no component is found.
    /// </summary>
    public static readonly Color IFFColor = Color.Gold;

    public const float SignatureAlpha = 1f;
    public const float MinSignatureSaturation = 0.8f;
    public const float MinSignatureValue = 0.65f;
    public const float MaxSignatureValue = 0.9f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public IFFFlags Flags = IFFFlags.None;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public IFFDesignation Designation = IFFDesignation.Other;

    /// <summary>
    /// Color for this to show up on IFF.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public Color Color = IFFColor;

    public static Color NormalizeSignatureColor(Color color)
    {
        var hsv = Color.ToHsv(color);
        var fallbackHue = Color.ToHsv(IFFColor).X;

        if (hsv.Y < MinSignatureSaturation)
        {
            hsv.X = fallbackHue;
            hsv.Y = MinSignatureSaturation;
        }

        hsv.Y = Math.Clamp(hsv.Y, MinSignatureSaturation, 1f);
        hsv.Z = Math.Clamp(hsv.Z, MinSignatureValue, MaxSignatureValue);
        hsv.W = SignatureAlpha;

        return Color.FromHsv(hsv);
    }
}

public enum IFFDesignation : byte
{
    Other,
    Ship,
    Station,
}

[Flags]
public enum IFFSortMode : byte
{
    None = 0,
    Other = 1,
    Ship = 2,
    Station = 4,
}

[Flags]
public enum IFFFlags : byte
{
    None = 0,

    /// <summary>
    /// Should the label for this grid be hidden at all ranges.
    /// </summary>
    HideLabel,

    /// <summary>
    /// Should the grid hide entirely (AKA full stealth).
    /// Will also hide the label if that is not set.
    /// </summary>
    Hide,

    // TODO: Need one that hides its outline, just replace it with a bunch of triangles or lines or something.
}
