using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._Persistence14.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PersistenceSaveGrid : LocalizedEntityCommands
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private IResourceManager _resource = default!;

    public override string Command => "persistencesave";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var intMapId))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[0])));
            return;
        }

        var mapId = new MapId(intMapId);
        if (!_map.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString("cmd-savemap-not-exist"));
            return;
        }

        var saveFilePath = (args.Length > 1 ? args[1] : null) ?? _config.GetCVar(CCVars.GameMap);
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            shell.WriteError(Loc.GetString("cmd-persistencesave-no-path", ("cvar", nameof(CCVars.GameMap))));
            return;
        }

        bool save_stat = _mapLoader.TrySaveMap(mapId, new ResPath(saveFilePath));
        shell.WriteLine(Loc.GetString("Did the map save? ") + $"{save_stat}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHintOptions(CompletionHelper.MapIds(args[0], entManager: EntityManager), Loc.GetString("cmd-hint-savemap-id"));
            case 2:
                var opts = CompletionHelper.UserFilePath(args[1], _resource.UserData);
                return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
        }
        return CompletionResult.Empty;
    }
}
