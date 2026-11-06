using System.IO;
using System.Linq;
using Content.Server.Administration;
using Content.Server.NPC.Queries.Considerations;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Toolshed;

namespace Content.Server._Persistence14.Toolshed;

[ToolshedCommand(Name = "entityeffect"), AdminCommand(Shared.Administration.AdminFlags.Debug)]
public sealed partial class EntityEffectCommand : ToolshedCommand
{
    private SharedEntityEffectsSystem? _effects;
    private ISerializationManager? _serialization;

    [CommandImplementation("prototype")]
    public EntityUid ApplyPrototype(
        [PipedArgument] EntityUid target,
        [CommandArgument] ProtoId<EntityEffectPrototype> effect,
        float scale = 1f)
    {
        _effects ??= GetSys<SharedEntityEffectsSystem>();

        _effects.TryApplyEffect(target, effect, scale);
        return target;
    }

    [CommandImplementation("prototype")]
    public IEnumerable<EntityUid> ApplyPrototype(
        [PipedArgument] IEnumerable<EntityUid> targets,
        [CommandArgument] ProtoId<EntityEffectPrototype> effect,
        float scale = 1f)
    {
        _effects ??= GetSys<SharedEntityEffectsSystem>();

        foreach (var target in targets)
        {
            _effects.TryApplyEffect(target, effect, scale);
            yield return target;
        }
    }

    [CommandImplementation("apply")]
    public void ApplyEffect(
        IInvocationContext ctx,
        [PipedArgument] EntityUid target,
        [CommandArgument] string yaml,
        [CommandArgument] float scale = 1f)
    {
        using var reader = new StringReader(yaml);
        var doc = DataNodeParser.ParseYamlStream(reader).Single();

        _serialization ??= IoCManager.Resolve<ISerializationManager>();
        _effects ??= GetSys<SharedEntityEffectsSystem>();

        // Confirm provided yaml string is valid.
        var validation = _serialization.ValidateNode<EntityEffect>(doc.Root);
        if (!validation.Valid)
        {
            foreach (var error in validation.GetErrors())
            {
                ctx.WriteLine($"{error.Node.Start}: {error.ErrorReason}");
            }
            return;
        }

        // Serialize and apply entity effect
        var effect = _serialization.Read<EntityEffect>(doc.Root, notNullableOverride: true);
        var applied = _effects.TryApplyEffect(target, effect, scale);
        if (applied) ctx.WriteLine("Effect applied!");
        else ctx.WriteLine("Unable to apply effect.");
    }

    public void ApplyEffect(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> targets,
        [CommandArgument] string yaml,
        [CommandArgument] float scale = 1f
    )
    {
        using var reader = new StringReader(yaml);
        var doc = DataNodeParser.ParseYamlStream(reader).Single();

        _serialization ??= IoCManager.Resolve<ISerializationManager>();
        _effects ??= GetSys<SharedEntityEffectsSystem>();

        // Confirm provided yaml string is valid.
        var validation = _serialization.ValidateNode<EntityEffect>(doc.Root);
        if (!validation.Valid)
        {
            foreach (var error in validation.GetErrors())
                ctx.WriteLine($"{error.Node.Start}: {error.ErrorReason}");
            return;
        }

        // Serialize and apply entity effect
        var effect = _serialization.Read<EntityEffect>(doc.Root, notNullableOverride: true);
        int count = 0;
        foreach (var ent in targets)
        {
            if (!_effects.TryApplyEffect(ent, effect, scale))
                ctx.WriteLine($"Unable to apply effect to entity \"{ent}\"");
            else count++;
        }

        ctx.WriteLine($"Effect applied to {count} entities.");
    }
}