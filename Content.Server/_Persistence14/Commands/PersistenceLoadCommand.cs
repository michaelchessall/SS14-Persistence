using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Persistence14.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PersistenceLoadCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private IResourceManager _resource = default!;

    public override string Command => "persistenceload";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var path = args[0];

        var loadId = new ResPath(path);
        bool save_stat = _mapLoader.TryLoadMap(loadId, out var entity, out var grids);
        shell.WriteLine(Loc.GetString("Did the thing load? ") + $"{save_stat}" + $"{entity}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var opts = CompletionHelper.UserFilePath(args[0], _resource.UserData)
                .Concat(CompletionHelper.ContentFilePath(args[0], _resource));
            return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
        }

        return CompletionResult.Empty;
    }
}
