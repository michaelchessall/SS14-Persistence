using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Crayon;
using Content.Shared.Database;
using Content.Shared.Paper;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Persistence.Paper;

public sealed partial class SignatureSystem : EntitySystem
{
    [Dependency] private SharedIdCardSystem _id = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<PaperComponent, PaperSignatureRequestMessage>(OnSignMessageReceived);
        SubscribeLocalEvent<PaperComponent, PaperSignatureFieldRequestMessage>(OnSignFieldMessageReceived);
    }

    private void OnGetVerbs(EntityUid uid, PaperComponent component, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using is not { } used)
            return;

        if (!TryComp<SignatureWriterComponent>(used, out var pen))
            return;
        var user = args.User;

        if (!TryGetSignatureName(user, out var signature))
            return;

        var fields = GetSignatureFields((uid, component));

        if (fields.Count == 0)
        {
            if (component.SignedBy.Contains(signature)) // Already signed by user
                return;

            var signVerb = new AlternativeVerb()
            {
                Text = Loc.GetString("verb-sign"),
                Act = () => SignAtIndex(user, (uid, component), (used, pen), signature: signature),
            };

            args.Verbs.Add(signVerb);
        }
        else
        {
            foreach (var field in fields)
            {
                var signVerb = new AlternativeVerb()
                {
                    Text = Loc.GetString("verb-sign-field", ("field", field)),
                    Act = () => SignAtField(user, (uid, component), field, (used, pen), signature: signature)
                };

                args.Verbs.Add(signVerb);
            }
        }
    }

    private void OnSignMessageReceived(EntityUid uid, PaperComponent component, ref PaperSignatureRequestMessage args)
    {
        SignAtIndex(args.Actor, (uid, component), replaceIndex: args.SignatureIndex);
    }

    private void OnSignFieldMessageReceived(Entity<PaperComponent> paper, ref PaperSignatureFieldRequestMessage args)
    {
        SignAtField(args.Actor, paper, args.SignatureField);
    }

    private void SignAtIndex(EntityUid user, Entity<PaperComponent> paper, Entity<SignatureWriterComponent>? pen = null, int replaceIndex = 0, string? signature = null)
    {
        if (signature == null && !TryGetSignatureName(user, out signature))
            return;

        SignatureIndexReplace(paper, signature, replaceIndex);
        SignatureStamp(paper, signature, pen);

        DoSignatureEffects(user, paper, signature);
    }

    private void SignAtField(EntityUid user, Entity<PaperComponent> paper, string field, Entity<SignatureWriterComponent>? pen = null, string? signature = null)
    {
        if (signature == null && !TryGetSignatureName(user, out signature))
            return;

        SignatureKeyReplace(paper, signature, field);
        SignatureStamp(paper, signature, pen);

        DoSignatureEffects(user, paper, signature);
    }

    private void DoSignatureEffects(EntityUid user, Entity<PaperComponent> paper, string signature)
    {
        paper.Comp.SignedBy.Add(signature);
        paper.Comp.EditingDisabled = true;
        Dirty(paper);

        // I don't love this being hardcoded but I wasn't entirely sure where to put it otherwise
        var signSound = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));
        _audio.PlayPvs(signSound, paper);
        _appearance.SetData(paper, PaperComponent.PaperVisuals.Signed, "signature");

        _adminLogger.Add(LogType.Chat, LogImpact.Low,
            $"{ToPrettyString(user):player} signed {ToPrettyString(paper):entity} with signature: {signature}");
    }

    /// <summary>
    /// Replaces an indexed signature field in the paper content with the signee's signature.
    /// If signed through verb, first index is used.
    /// </summary>
    private void SignatureIndexReplace(Entity<PaperComponent> paper, string signature, int index)
    {
        var newText = PaperTagUtility.ReplaceNthTag(paper.Comp.Content, "[signature]", index, $"{signature}");
        _paper.SetContent(paper, newText);
    }

    /// <summary>
    /// Replaces a signature field with a specific key.
    /// </summary>
    private void SignatureKeyReplace(Entity<PaperComponent> paper, string signature, string key)
    {
        var newText = PaperTagUtility.ReplaceAllTag(paper.Comp.Content, $"[signature=\"{key}\"]", $"{signature}");
        _paper.SetContent(paper, newText);
    }

    /// <summary>
    /// Applies the signature stamp to the paper.
    /// </summary>
    private void SignatureStamp(Entity<PaperComponent> paper, string signature, Entity<SignatureWriterComponent>? pen = null)
    {
        // Only stamp once per signature.
        if (paper.Comp.SignedBy.Contains(signature))
            return;

        var color = pen?.Comp.SignatureColor ?? Color.Black;
        if (pen?.Comp.SignatureColor == null && TryComp<CrayonComponent>(pen, out var crayon))
            color = crayon.Color;

        var stamp = new StampDisplayInfo()
        {
            StampedName = signature,
            StampedColor = color,
            UseBox = false,
            UseNameAsLoc = false,
        };

        paper.Comp.StampedBy.Add(stamp);
        Dirty(paper);
    }

    private bool TryGetSignatureName(EntityUid uid, [NotNullWhen(true)] out string? signatureName)
    {
        signatureName = null;

        if (_id.TryFindIdCard(uid, out var id))
        {
            signatureName = id.Comp.FullName;
            return signatureName != null;
        }

        signatureName = Name(uid);
        return signatureName != null;
    }

    private static readonly Regex SignatureRegex
        = new(@"\[signature=(?:""(?<field>[^""]+)""|(?<field>[^\]\s]+))[^\]]*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private List<string> GetSignatureFields(Entity<PaperComponent> paper)
    {
        HashSet<string> fields = new();
        var matches = SignatureRegex.Matches(paper.Comp.Content);

        foreach (Match match in matches)
        {
            var field = match.Groups["field"].Value.Trim();

            if (string.IsNullOrWhiteSpace(field))
                continue;

            fields.Add(field);
        }

        return fields.Order(StringComparer.OrdinalIgnoreCase).ToList();
    }
}

[Serializable, NetSerializable]
public sealed class PaperSignatureRequestMessage(int signatureIndex) : BoundUserInterfaceMessage
{
    public readonly int SignatureIndex = signatureIndex;
}

[Serializable, NetSerializable]
public sealed class PaperSignatureFieldRequestMessage(string field) : BoundUserInterfaceMessage
{
    public readonly string SignatureField = field;
}