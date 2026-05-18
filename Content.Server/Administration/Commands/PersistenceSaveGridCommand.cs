using Content.Server.Persistence.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed partial class PersistenceSaveGridCommand : LocalizedEntityCommands
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private PersistenceSystem _persistence = default!;

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

        var uid = _ent.GetEntity(uidNet);

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

}
