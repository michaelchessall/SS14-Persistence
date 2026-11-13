using Content.Shared.Chat;
using Content.Shared.Speech;
using System.Text.RegularExpressions;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class AccentSystem : EntitySystem
{
    public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?‽])(?![\.!\?‽])", RegexOptions.Compiled);

    [SubscribeLocalEvent]
    private void AccentHandler(TransformSpeechEvent args)
    {
        if (args.Cancelled)
            return;

        var accentEvent = new AccentGetEvent(args.Sender, args.Message);

        RaiseLocalEvent(args.Sender, ref accentEvent);
        args.Message = accentEvent.Message;
    }
}
