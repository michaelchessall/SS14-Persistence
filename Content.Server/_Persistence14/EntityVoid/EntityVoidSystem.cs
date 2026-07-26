using Content.Shared._Persistence14.EntityVoid;
using Content.Shared._Persistence14.PersistentIdentifier;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._Persistence14.EntityVoid;

/// <summary>
/// A system for shucking entities into the void to be handled later. Set up to allow persistent reference to these entities through a GUID.
/// </summary>
public sealed partial class EntityVoidSystem : EntitySystem
{
    [Dependency] private PersistentIdentifierSystem _pid = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private PhysicsSystem _physics = default!;

    /// <summary>
    /// Attempts to serialize an entity and remove it from the world for storage.
    /// </summary>

    public bool TryVoidEntity(EntityUid uid)
    {
        if (!Exists(uid))
            return false;

        if (HasComp<VoidedComponent>(uid)) // Don't void twice, if voided already... something is wrong.
            return false;

        AddComp<VoidedComponent>(uid);

        var id = _pid.EnsureId(uid); // Piggy backing off of PID for the GUID system. Keeps things symetric.
        var path = BuildPath(id);

        if (!_mapLoader.TrySaveEntity(uid, path))
            return false;

        QueueDel(uid);
        return true;
    }

    /// <summary>
    /// Attempts to regenerate a given ID's entity from the serialized file and place it at a given set of coordiantes.
    /// </summary>
    public bool TryReconstructEntity(string persistentId, MapCoordinates coordinates, out EntityUid uid)
    {
        uid = EntityUid.Invalid;


        var path = BuildPath(persistentId);

        if (!_mapLoader.TryLoadEntity(path, out var entity))
            return false;


        uid = entity.Value.Owner;
        _transform.SetMapCoordinates(uid, coordinates);
        _physics.WakeBody(uid);

        if (HasComp<VoidedComponent>(uid))
            RemComp<VoidedComponent>(uid);


        RemoveEntity(persistentId);

        return true;
    }

    /// <summary>
    /// Turns an id into a path to be consumed by other void functions.
    /// </summary>
    private ResPath BuildPath(string persistentId) => new ResPath($"/EntityVoid/{persistentId}");

    /// <summary>
    /// Removes an entity file from storage, functionally deleting it.
    /// </summary>
    public void RemoveEntity(string persistentId)
    {
        var path = BuildPath(persistentId);

        if (_resourceManager.UserData.Exists(path))
            _resourceManager.UserData.Delete(path);
    }
}