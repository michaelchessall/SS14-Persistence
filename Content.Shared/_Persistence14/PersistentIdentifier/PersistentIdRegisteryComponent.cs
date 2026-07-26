using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._Persistence14.PersistentIdentifier;

[RegisterComponent]
public sealed partial class PersistentIdRegisterComponent : Component
{
    /// <summary>
    /// A runtime register of registered entities allowing for faster lookup.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    private Dictionary<string, Entity<PersistentIdentifierComponent>> _registeredEntities = new();

    [DataField]
    public bool Global = false;

    /// <summary>
    /// Verifies the existance of a valid entity within the register matching a provided key.
    /// </summary>
    public bool Contains(string id, IEntityManager entMan) => TryGet(id, out _, entMan);

    /// <summary>
    /// Attempts to retrieve a valid entity matching a provided key from the register.<br/>
    /// Culls any stale references that either do not exist or do not contain the required components/state.
    /// </summary>
    public bool TryGet(string id, out Entity<PersistentIdentifierComponent> ent, IEntityManager entMan)
    {
        ent = default!;

        if (!_registeredEntities.TryGetValue(id, out ent))
            return false;

        if (!entMan.EntityExists(ent) || !entMan.TryGetComponent<PersistentIdentifierComponent>(ent, out var idComp) || idComp.Id != id)
        {
            _registeredEntities.Remove(id);
            ent = default!;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Culls any stale references that either do not exist or do not contain the required components/state.
    /// </summary>
    public bool CullStaleEntities(IEntityManager entMan)
    {
        var ids = _registeredEntities.Keys;
        bool cullAny = false;

        List<string> staleIds = new();

        foreach (var id in ids)
        {
            var ent = _registeredEntities[id];
            if (!entMan.EntityExists(ent) || !entMan.TryGetComponent<PersistentIdentifierComponent>(ent, out var idComp) || idComp.Id != id)
            {
                staleIds.Add(id);
                cullAny = true;
            }
        }

        foreach (var id in staleIds)
            _registeredEntities.Remove(id);

        return cullAny;
    }

    /// <summary>
    /// Attempts to register an entity to the register.<br/><br/>
    /// Return false if a matching entity already exists in the register, otherwise true.
    /// </summary>
    public bool TryRegister(Entity<PersistentIdentifierComponent> ent, IEntityManager entMan)
    {
        if (Contains(ent.Comp.Id, entMan) || !ent.Comp.IdInit) return false;

        _registeredEntities[ent.Comp.Id] = ent;
        return true;
    }

    /// <summary>
    /// Attempts to remove an ID from the register.
    /// Returns true if the ID was successfully removed, otherwise false.
    /// </summary>
    public bool TryUnregister(string id) => _registeredEntities.Remove(id);

    /// <summary>
    /// Attempts to remove a <see cref="PersistentIdentifierComponent"/> from the register. 
    /// Returns true if the ID was successfully removed, otherwise false.
    /// </summary>
    /// <param name="ent"></param>
    /// <returns></returns>
    public bool TryUnregister(Entity<PersistentIdentifierComponent> ent) => TryUnregister(ent.Comp.Id);

    /// <summary>
    /// Removes all entities from the registry.
    /// </summary>
    public void Clear() => _registeredEntities.Clear();
}