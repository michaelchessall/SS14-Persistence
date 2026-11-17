using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Persistence14.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PersistenceLoadCharCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private IResourceManager _resource = default!;

    public override string Command => "persistenceloadchar";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var path = args[0];

        var loadId = new ResPath(path);
        bool save_stat = _mapLoader.TryLoadEntity(loadId, out var entity);
        shell.WriteLine(Loc.GetString("Did the thing load? ") + $"{save_stat}" + $"{entity}");
        var player = shell.Player;

        if (player == null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (player.AttachedEntity == null)
        {
            shell.WriteLine(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        EntityUid pe = player.AttachedEntity.Value;
        var coords = _entManager.GetComponent<TransformComponent>(pe).Coordinates;
        if (entity != null)
        {
            _transform.SetCoordinates(entity.Value, coords);
        }
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
