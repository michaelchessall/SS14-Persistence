using System.Globalization;
using System.IO;
using Content.Shared._Persistence14.RandomTable.ValueDefinition;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._Persistence14.RandomTable;

[TypeSerializer]
public sealed class RandomTableTypeSerializer :
    ITypeReader<RandomTableSelector, MappingDataNode>
{
    public RandomTableSelector Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<RandomTableSelector>? instanceProvider = null)
    {
        if (node.Has("value")) return ReadAsValue(serializationManager, node, dependencies, hookCtx, context, instanceProvider);
        if (node.Has("prototype")) return ReadAsPrototype(serializationManager, node, dependencies, hookCtx, context, instanceProvider);

        throw new InvalidDataException("Custom validation not supported! Please specify the type manually!");
    }

    private RandomTableSelector ReadAsValue(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<RandomTableSelector>? instanceProvider = null)
    {
        if (!node.Has("value"))
            throw new InvalidDataException("Custom validation not supported! Please specify the type manually!");

        var dataNode = node["value"];
        if (dataNode is not ValueDataNode valueNode)
            throw new InvalidDataException("Custom validation not supported! Please specify the type manually!");

        var value = valueNode.Value;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            return (RandomTableIntValueDefinition)intValue;

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal))
            return (RandomTableFloatValueDefinition)floatVal;

        return (RandomTableStringValueDefinition)value; // If it doesn't parse, its probably a string.
    }

    private RandomTableSelector ReadAsPrototype(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<RandomTableSelector>? instanceProvider = null)
    {
        if (!node.Has("prototype"))
            throw new InvalidDataException("Custom validation not supported! Please specify the type manually!");

        var dataNode = node["prototype"];
        if (dataNode is not ValueDataNode valueNode)
            throw new InvalidDataException("Custom validation not supported! Please specify the type manually!");

        var value = valueNode.Value;

        return (RandomTablePrototypeValueDefinition)value;
    }

    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (node.Has("value")) return ValidateAsValue(serializationManager, node, dependencies, context);
        if (node.Has("prototype")) return ValidateAsPrototype(serializationManager, node, dependencies, context);

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    private ValidationNode ValidateAsValue(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (!node.Has("value"))
            return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");

        var dataNode = node["value"];
        if (dataNode is not ValueDataNode valueNode)
            return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");

        var value = valueNode.Value;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            return serializationManager.ValidateNode<RandomTableIntValueDefinition>(node, context);

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal))
            return serializationManager.ValidateNode<RandomTableFloatValueDefinition>(node, context);

        return serializationManager.ValidateNode<RandomTableStringValueDefinition>(node, context);
    }

    private ValidationNode ValidateAsPrototype(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (!node.Has("prototype"))
            return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");

        var dataNode = node["prototype"];
        if (dataNode is not ValueDataNode)
            return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");

        return serializationManager.ValidateNode<RandomTablePrototypeValueDefinition>(node, context);
    }
}

[TypeSerializer]
public sealed class RandomTableValueSerializer : ITypeReader<RandomTableSelector, ValueDataNode>
{
    public RandomTableSelector Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<RandomTableSelector>? instanceProvider = null)
    {
        if (int.TryParse(node.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            return (RandomTableIntValueDefinition)intValue;

        if (float.TryParse(node.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal))
            return (RandomTableFloatValueDefinition)floatVal;

        return (RandomTableStringValueDefinition)node.Value; // If it doesn't parse, its probably a string.
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedValueNode(node);
    }
}