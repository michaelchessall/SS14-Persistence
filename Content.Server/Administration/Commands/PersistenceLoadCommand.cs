using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed partial class PersistenceLoadCommand : LocalizedEntityCommands
{
    [Dependency] private MapLoaderSystem _mapLoader = default!;

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
}
