using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Mind.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Persistence14.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PersistenceSaveCharCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private IResourceManager _resource = default!;

    public override string Command => "persistencesavechar";

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

        var entId = new EntityUid(intMapId);


        var saveFilePath = (args.Length > 1 ? args[1] : null) ?? _config.GetCVar(CCVars.GameMap);
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            shell.WriteError(Loc.GetString("cmd-persistencesave-no-path", ("cvar", nameof(CCVars.GameMap))));
            return;
        }

        bool save_stat = _mapLoader.TrySaveGeneric(entId, new ResPath(saveFilePath), out var category);
        shell.WriteLine(Loc.GetString("Did the map save? ") + $"{save_stat}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHintOptions(CompletionHelper.Components<MindContainerComponent>(args[0], entManager: EntityManager), Loc.GetString("cmd-hint-savechar-id"));
            case 2:
                var opts = CompletionHelper.UserFilePath(args[1], _resource.UserData);
                return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
        }
        return CompletionResult.Empty;
    }
}
