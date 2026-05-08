using Content.Server.Speech.Components;
using Content.Shared.Speech;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class GoblinAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoblinAccentComponent, AccentGetEvent>(OnAccent);;
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    public string Accentuate(string message, GoblinAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "goblin");
        return msg;
    }

    private void OnAccent(EntityUid uid, GoblinAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
