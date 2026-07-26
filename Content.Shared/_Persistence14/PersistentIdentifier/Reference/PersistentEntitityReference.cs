using Robust.Shared.Serialization;

namespace Content.Shared._Persistence14.PersistentIdentifier.Reference;

[DataDefinition, NetSerializable, Serializable]
public partial record struct PersistentEntityReference
{
    [DataField("targetId", readOnly: true)] private string? _targetId;
    public string TargetId => _targetId ?? PersistentIdentifierSystem.EmptyId;

    public PersistentEntityReference()
    {
        _targetId = null;
    }

    public PersistentEntityReference(string targetId)
    {
        _targetId = targetId;
    }

    public static implicit operator PersistentEntityReference(string targetId) => new(targetId);
    public static implicit operator string(PersistentEntityReference reference) => reference.TargetId;
};