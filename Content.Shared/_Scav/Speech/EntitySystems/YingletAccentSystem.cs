using System.Text.RegularExpressions;
using Content.Shared._Scav.Speech.Components;
using Content.Shared.Speech.EntitySystems;

namespace Content.Shared._Scav.Speech.EntitySystems;

public sealed class YingletAccentSystem : RelayAccentSystem<YingletAccentComponent>
{
    // @formatter:off
    private static readonly Regex RegexLowerTh = new Regex("th{1,3}");
    private static readonly Regex RegexUpperTh = new Regex("Th{1,3}");
    private static readonly Regex RegexFullUpperTh = new Regex("TH{1,3}");
    // @formatter:on

    public override string Accentuate(string message, Entity<YingletAccentComponent>? ent = null)
    {
        // zhis is fun
        message = RegexLowerTh.Replace(message, "zh");
        message = RegexUpperTh.Replace(message, "Zh");
        message = RegexFullUpperTh.Replace(message, "ZH");

        return message;
    }
}
