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
    [Dependency] private readonly SectorWeatherSpawnSystem _sectorWeatherSpawns = default!;

    public override string Command => "sectorweather";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine("Usage: sectorweather <Sector> <WeatherPrototypeId|clear|bypass>");
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

        if (string.Equals(value, "bypass", StringComparison.OrdinalIgnoreCase))
        {
            var activeWeather = _sectorWeather.GetWeatherSnapshot();
            if (!activeWeather.TryGetValue(sector, out var activeWeatherId))
            {
                shell.WriteError($"No active weather found for sector {sector}.");
                return;
            }

            if (_sectorWeatherSpawns.BypassCooldown(sector, activeWeatherId))
                shell.WriteLine($"Bypassed cooldown for sector {sector}.");
            else
                shell.WriteError($"Failed to bypass cooldown for sector {sector}.");

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
                CompletionHelper.PrototypeIDs<SectorWeatherPrototype>().Append(new CompletionOption("clear")).Append(new CompletionOption("bypass")),
                "Weather prototype ID or clear"),
            _ => CompletionResult.Empty,
        };
    }
}
