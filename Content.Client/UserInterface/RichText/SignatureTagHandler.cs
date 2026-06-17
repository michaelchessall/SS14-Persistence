using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Converts [signature] tags into clickable buttons that sign with the player's name.
/// </summary>
public sealed class SignatureTagHandler : IMarkupTagHandler
{
    public string Name => "signature";

    private static int _signatureCounter;

    public static void ResetSignatureCounter() => _signatureCounter = 0;

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public string TextBefore(MarkupNode node) => "";
    public string TextAfter(MarkupNode node) => "";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        var hasField = TryGetSignatureField(node, out var field);

        var btn = new Button
        {
            Text = hasField ? field : Loc.GetString("paper-signature-sign-button"),
            MinSize = new Vector2(48, PaperTagHelper.FontLineHeight + 4),
            MaxSize = new Vector2(100, PaperTagHelper.FontLineHeight + 4),
            Margin = new Thickness(1, 2, 1, 2),
            StyleClasses = { "ButtonSquare" },
            TextAlign = Label.AlignMode.Center,
            Name = $"signature_{(hasField ? field : _signatureCounter++)}"
        };

        btn.OnPressed += _ =>
        {
            if (PaperTagHelper.FindPaperWindow(btn) is { } paperWindow)
            {
                if (hasField)
                {
                    paperWindow.SendFieldSignatureRequest(field ?? ""); // It won't be null here... but appeasing the editor.
                }
                else
                {
                    var buttonIndex = PaperTagHelper.CountButtonsBefore(btn, b => b.Text == Loc.GetString("paper-signature-sign-button"));
                    paperWindow.SendSignatureRequest(buttonIndex);
                }
            }
        };

        control = btn;
        return true;
    }

    private bool TryGetSignatureField(MarkupNode node, [NotNullWhen(true)] out string? field)
    {
        field = null;
        if (node.Value.TryGetString(out field))
        {
            return !string.IsNullOrWhiteSpace(field);
        }
        return false;
    }
}
