using System.Linq;
using System.Numerics;
using Content.Server.Administration;
using Content.Server.Persistence.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._Persistence14.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PersistenceLoadGridCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntitySystemManager _system = default!;
    [Dependency] private readonly PersistenceSystem _persistence = default!;
    [Dependency] private IResourceManager _resource = default!;

    public override string Command => "persistenceloadgrid";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2 || args.Length == 3 || args.Length > 6)
        {
            shell.WriteError("Must have either 2, 4, 5, or 6 arguments.");
            return;
        }

        if (!int.TryParse(args[0], out var intMapId))
        {
            shell.WriteError($"{args[0]} is not a valid integer.");
            return;
        }

        var mapId = new MapId(intMapId);

        // no loading into null space
        if (mapId == MapId.Nullspace)
        {
            shell.WriteError("Cannot load into nullspace.");
            return;
        }

        var sys = _system.GetEntitySystem<SharedMapSystem>();
        if (!sys.MapExists(mapId))
        {
            shell.WriteError($"MapID {intMapId} did not exist, creating without map init");
            sys.CreateMap(mapId, false); // doesnt runmapinit to be conservative.
        }

        Vector2 offset = default;
        if (args.Length >= 4)
        {
            if (!float.TryParse(args[2], out var x))
            {
                shell.WriteError($"{args[2]} is not a valid float.");
                return;
            }

            if (!float.TryParse(args[3], out var y))
            {
                shell.WriteError($"{args[3]} is not a valid float.");
                return;
            }

            offset = new Vector2(x, y);
        }

        Angle rot = default;
        if (args.Length >= 5)
        {
            if (!float.TryParse(args[4], out var rotation))
            {
                shell.WriteError($"{args[4]} is not a valid float.");
                return;
            }

            rot = Angle.FromDegrees(rotation);
        }

        var opts = DeserializationOptions.Default;
        if (args.Length >= 6)
        {
            if (!bool.TryParse(args[5], out var storeUids))
            {
                shell.WriteError($"{args[5]} is not a valid boolean.");
                return;
            }

            opts.StoreYamlUids = storeUids;
        }

        if (!_persistence.LoadGrid(new ResPath(args[1]), mapId, offset, rot, out var errorMessage, out var grid, opts))
        {
            shell.WriteError("There was a problem while loading the grid.");
            if (!string.IsNullOrWhiteSpace(errorMessage))
                shell.WriteError(errorMessage);
        }
        else
        {
            shell.WriteLine($"Grid successfuly loaded with id '{grid}' on Map {mapId} {offset} ({rot.Degrees}º) ");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-id"));
            case 2:
                var opts = CompletionHelper.UserFilePath(args[1], _resource.UserData)
                    .Concat(CompletionHelper.ContentFilePath(args[1], _resource));
                return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
            case 3:
                return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-x-position"));
            case 4:
                return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-y-position"));
            case 5:
                return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-rotation"));
            case 6:
                return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-force"));
        }

        return CompletionResult.Empty;
    }
}
