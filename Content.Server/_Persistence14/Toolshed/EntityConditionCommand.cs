using System.IO;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Toolshed;

namespace Content.Server._Persistence14.Toolshed;

[ToolshedCommand(Name = "entitycondition"), AdminCommand(Shared.Administration.AdminFlags.Debug)]
public sealed partial class EntityConditionCommands : ToolshedCommand
{
    private SharedEntityConditionsSystem? _conditions;
    private ISerializationManager? _serialization;

    [CommandImplementation("whereprototype")]
    public IEnumerable<EntityUid> Where(
        [PipedArgument] IEnumerable<EntityUid> targets,
        [CommandArgument] ProtoId<EntityConditionPrototype> condition)
    {
        _conditions ??= GetSys<SharedEntityConditionsSystem>();

        foreach (var target in targets)
        {
            if (_conditions.TryCondition(target, condition))
                yield return target;
        }
    }

    [CommandImplementation("wherenotprototype")]
    public IEnumerable<EntityUid> WhereNot(
        [PipedArgument] IEnumerable<EntityUid> targets,
        [CommandArgument] ProtoId<EntityConditionPrototype> condition)
    {
        _conditions ??= GetSys<SharedEntityConditionsSystem>();

        foreach (var target in targets)
        {
            if (!_conditions.TryCondition(target, condition))
                yield return target;
        }
    }

    [CommandImplementation("where")]
    public IEnumerable<EntityUid> Where(
    IInvocationContext ctx,
    [PipedArgument] IEnumerable<EntityUid> targets,
    [CommandArgument] string yaml
)
    {
        _conditions ??= GetSys<SharedEntityConditionsSystem>();
        _serialization ??= IoCManager.Resolve<ISerializationManager>();

        using var reader = new StringReader(yaml);
        var doc = DataNodeParser.ParseYamlStream(reader).Single();

        // Confirm provided yaml string is valid.
        var validation = _serialization.ValidateNode<EntityCondition>(doc.Root);
        if (!validation.Valid)
        {
            foreach (var error in validation.GetErrors())
                ctx.WriteLine($"{error.Node.Start}: {error.ErrorReason}");
            yield break;
        }

        // Parse and apply
        var condition = _serialization.Read<EntityCondition>(doc.Root, notNullableOverride: true);
        foreach (var ent in targets)
        {
            if (_conditions.TryCondition(ent, condition))
                yield return ent;
        }
    }

    [CommandImplementation("wherenot")]
    public IEnumerable<EntityUid> WhereNot(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> targets,
        [CommandArgument] string yaml
    )
    {
        _conditions ??= GetSys<SharedEntityConditionsSystem>();
        _serialization ??= IoCManager.Resolve<ISerializationManager>();

        using var reader = new StringReader(yaml);
        var doc = DataNodeParser.ParseYamlStream(reader).Single();

        // Confirm provided yaml string is valid.
        var validation = _serialization.ValidateNode<EntityCondition>(doc.Root);
        if (!validation.Valid)
        {
            foreach (var error in validation.GetErrors())
                ctx.WriteLine($"{error.Node.Start}: {error.ErrorReason}");
            yield break;
        }

        // Parse and apply
        var condition = _serialization.Read<EntityCondition>(doc.Root, notNullableOverride: true);
        foreach (var ent in targets)
        {
            if (!_conditions.TryCondition(ent, condition))
                yield return ent;
        }
    }
}