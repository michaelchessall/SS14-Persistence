using System.Linq;
using Content.Server.Sectors.Systems;
using Content.Shared.Administration;
using Content.Shared.Sectors;
using Content.Shared.Sectors.Prototypes;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SectorWeatherCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SectorWeatherSystem _sectorWeather = default!;

    public override string Command => "sectorweather";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine("Usage: sectorweather <Sector> <WeatherPrototypeId|clear>");
            return;
        }

        if (!Enum.TryParse<SpaceSector>(args[0], true, out var sector))
        {
            shell.WriteError($"Invalid sector '{args[0]}'.");
            return;
        }

        var value = args[1];
        if (string.Equals(value, "clear", StringComparison.OrdinalIgnoreCase))
        {
            if (_sectorWeather.ClearWeather(sector))
                shell.WriteLine($"Cleared sector weather for {sector}.");
            else
                shell.WriteLine($"No active weather found for {sector}.");

            return;
        }

        if (!_sectorWeather.TrySetWeather(sector, value))
        {
            shell.WriteError($"Unknown sector weather prototype '{value}'.");
            return;
        }

        shell.WriteLine($"Set {sector} weather to {value}.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var sectorOptions = Enum.GetNames<SpaceSector>();

        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(sectorOptions, "Sector name"),
            2 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<SectorWeatherPrototype>().Append(new CompletionOption("clear")),
                "Weather prototype ID or clear"),
            _ => CompletionResult.Empty,
        };
    }
}
