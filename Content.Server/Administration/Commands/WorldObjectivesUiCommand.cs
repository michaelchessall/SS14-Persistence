using Content.Server.CrewAssignments.AdminUI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class WorldObjectivesUiCommand : LocalizedEntityCommands
{
    [Dependency] private EuiManager _euiManager = default!;

    public override string Command => "worldobjectivesui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new AdminWorldObjectivesEui();
        _euiManager.OpenEui(ui, player);
    }
}
