using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._Persistence14.PersistentIdentifier.Reference;

[TypeSerializer]
public sealed class PerssitentEntityReferenceValueSerializer : ITypeReader<PersistentEntityReference, ValueDataNode>, ITypeWriter<PersistentEntityReference>
{
    public PersistentEntityReference Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<PersistentEntityReference>? instanceProvider = null)
    {
        return node.Value;
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedValueNode(node);
    }

    public DataNode Write(ISerializationManager serializationManager, PersistentEntityReference value, IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return new ValueDataNode(value);
    }
}