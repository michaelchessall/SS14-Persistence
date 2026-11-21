using Content.Server.Administration;
using Content.Server.Persistence.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server._Persistence14.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PersistenceSaveGridCommand : LocalizedEntityCommands
{
    [Dependency] private readonly PersistenceSystem _persistence = default!;
    [Dependency] private IResourceManager _resource = default!;

    public override string Command => "persistencesavegrid";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError("Not enough arguments.");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet))
        {
            shell.WriteError("Not a valid entity ID.");
            return;
        }

        var uid = EntityManager.GetEntity(uidNet);

        if (_persistence.SaveGrid(uid, new ResPath(args[1]), out var errorMessage, dumpSpecialEntities: true, deleteGrid: true))
        {
            shell.WriteLine("Save successful. Look in the user data directory.");
        }
        else
        {
            shell.WriteError("Save unsuccessful!");
            if (!string.IsNullOrWhiteSpace(errorMessage))
                shell.WriteError(errorMessage);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHintOptions(CompletionHelper.Components<MapGridComponent>(args[0], EntityManager), Loc.GetString("cmd-hint-savebp-id"));
            case 2:
                var opts = CompletionHelper.UserFilePath(args[1], _resource.UserData);
                return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
        }
        return CompletionResult.Empty;
    }
}
