namespace Content.Shared._Persistence14.PersistentIdentifier.Reference;

[DataDefinition]
public partial struct PersistentEntityReference
{
    [DataField(readOnly: true), ViewVariables(VVAccess.ReadOnly)]
    public string TargetId = PersistentIdentifierSystem.EmptyId;

    [ViewVariables(VVAccess.ReadOnly)]
    private Entity<PersistentIdentifierComponent>? _cachedEntity = null;

    public PersistentEntityReference() { }

    /// <summary>
    /// Attempts to resolve the reference into an entity with a matching ID.
    /// </summary>
    /// <returns>True if the reference succesfully resolves into an entity, otherwise false.</returns>
    public bool TryResolve(PersistentIdentifierSystem pid, out Entity<PersistentIdentifierComponent> entity, ISawmill? sawmill = null)
    {
        if (sawmill is { }) sawmill.Info("Attempting to resolve ID reference.");
        if (_cachedEntity is { } cached && cached.Comp.Id == TargetId)
        {
            entity = cached;
            if (sawmill is { }) sawmill.Info("Cached entity found, returning.");
            return true;
        }
        if (!pid.TryFetchId(TargetId, out entity, sendLogs: true))
        {
            return false;
        }
        if (entity.Comp.Id != TargetId)
        {
            if (sawmill is { }) sawmill.Info($"Fetched ID does not match target id... somehow (Expected:{TargetId}, Actual: {entity.Comp.Id}).");
            return false;
        }

        _cachedEntity = entity;
        if (sawmill is { }) sawmill.Info("Entity cached and valid.");
        return true;
    }
    public void Reset()
    {
        TargetId = PersistentIdentifierSystem.EmptyId;
        _cachedEntity = null;
    }
}