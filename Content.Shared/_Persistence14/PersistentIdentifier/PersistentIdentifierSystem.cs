using Content.Shared._Persistence14.PersistentIdentifier.Reference;

namespace Content.Shared._Persistence14.PersistentIdentifier;

public sealed partial class PersistentIdentifierSystem : EntitySystem
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private ILogManager _log = default!;

    /// <summary>
    /// The Sawmill key for all ID related log messages.
    /// </summary>
    public const string Sawmill = "persistent-id";

    public static string EmptyId = Guid.Empty.ToString();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PersistentIdRegisterComponent, PersistentIdChangedEvent>(OnRegisterIdChange);
    }

    /// <summary>
    /// Gets the ID from a <see cref="PersistentIdentifierComponent"/>. Generates a new ID if one is not present.
    /// </summary>
    public string EnsureId(Entity<PersistentIdentifierComponent> ent)
    {
        if (ent.Comp.IdInit) return ent.Comp.Id;

        ResetId(ent, out var id, PersistentIdChangeBehaviour.Sever);
        return id;
    }

    public string EnsureId(EntityUid uid) => EnsureId(uid, out _);
    public string EnsureId(EntityUid uid, out Entity<PersistentIdentifierComponent> ent)
    {
        EnsureComp<PersistentIdentifierComponent>(uid, out var idComp);
        ent = (uid, idComp);
        return EnsureId((uid, idComp));
    }

    /// <summary>
    /// Attempts to retrieve the ID from a <see cref="PersistentIdentifierComponent"/>.<br/><br/>
    /// Return true if the component has an ID, otherwise false.
    /// </summary>
    public bool TryGetId(Entity<PersistentIdentifierComponent> ent, out string id)
    {
        if (ent.Comp.IdInit)
        {
            id = ent.Comp.Id;
            return true;
        }

        id = EmptyId;
        return false;
    }

    /// <summary>
    /// Resets the ID of a <see cref="PersistentIdentifierComponent"/> to a new valid GUID.
    /// </summary>
    public void ResetId(Entity<PersistentIdentifierComponent> ent, out string id, PersistentIdChangeBehaviour behaviour = PersistentIdChangeBehaviour.Sever)
    {
        var oldId = ent.Comp.Id;
        ent.Comp.Id = Guid.NewGuid().ToString();
        id = ent.Comp.Id;
        Dirty(ent);

        var ev = new PersistentIdChangedEvent(ent, oldId, ent.Comp.Id, behaviour);
        RaiseLocalEvent(ref ev);
    }

    public void ResetId(Entity<PersistentIdentifierComponent> ent, PersistentIdChangeBehaviour behaviour = PersistentIdChangeBehaviour.Sever)
        => ResetId(ent, out _, behaviour);

    /// <summary>
    /// Empties the ID of a <see cref="PersistentIdentifierComponent"/>.
    /// </summary>
    public void ClearId(Entity<PersistentIdentifierComponent> ent, PersistentIdChangeBehaviour behaviour = PersistentIdChangeBehaviour.Sever)
    {
        if (!ent.Comp.IdInit) return;

        var oldId = ent.Comp.Id;
        ent.Comp.Id = EmptyId;
        Dirty(ent);

        var ev = new PersistentIdChangedEvent(ent, oldId, ent.Comp.Id, behaviour);
        RaiseLocalEvent(ref ev);
    }

    public bool OverrideId(Entity<PersistentIdentifierComponent> ent, string id, PersistentIdChangeBehaviour behaviour = PersistentIdChangeBehaviour.Sever)
    {
        if (id == ent.Comp.Id) return false;

        if (IsEmptyId(id))
        {
            _log.GetSawmill(Sawmill).Warning("Unable to override ID to empty. Use ClearId instead.");
            return false;
        }

        var oldId = ent.Comp.Id;
        ent.Comp.Id = id;
        Dirty(ent);

        var ev = new PersistentIdChangedEvent(ent, oldId, ent.Comp.Id, behaviour);
        RaiseLocalEvent(ref ev);
        return true;
    }

    /// <summary>
    /// Attempts to resolve a given id on a source entity. Will prioritize an existing <see cref="PersistentIdRegisterComponent"/> and may add one if none are available.
    /// </summary>
    /// <param name="sourceUid">The Uid of the entity to look into.</param>
    /// <param name="id">The desired id.</param>
    /// <param name="ent">The output variable storing the retrieved entity.</param>
    /// <param name="conditional">A conditional function applied to the search.</param>
    /// <param name="useFetchIfFalse">If true, the resolve method will attempt to fetch the entity using <see cref="TryFetchId"/> if unable to resolve using the source registry.</param>
    /// <param name="ensureRegistry">If true, the resolve method will ensure the existence of a <see cref="PersistentIdRegisterComponent"/> on the source Uid.</param> 
    /// <returns>True if able to successfully resolve the id, otherwise false.</returns>
    public bool TryResolveId(
        EntityUid sourceUid,
        string id,
        out Entity<PersistentIdentifierComponent> ent,
        Func<Entity<PersistentIdentifierComponent>, bool>? conditional = null,
        bool useFetchIfFalse = true, bool ensureRegistry = true)
    {
        ent = default!;
        conditional ??= _ => true;

        PersistentIdRegisterComponent? registry;
        if (ensureRegistry)
        {
            EnsureComp<PersistentIdRegisterComponent>(sourceUid, out registry);
            if (registry.TryGet(id, out ent, _entMan) && conditional(ent))
                return true;
        }
        else if (
            TryComp<PersistentIdRegisterComponent>(sourceUid, out registry) &&
            registry.TryGet(id, out ent, _entMan) &&
            conditional(ent))
        {
            return true;
        }

        if (useFetchIfFalse)
            return TryFetchId(id, out ent, conditional, registry);
        return false;
    }

    /// <summary>
    /// Attempts to resolve a provided <see cref="PersistentEntityReference"/> into an entity.
    /// </summary>
    /// <returns>True if the reference sucessfully resolved into an entity, otherwise false.</returns>
    public bool TryResolveId(PersistentEntityReference reference, out Entity<PersistentIdentifierComponent> ent)
        => reference.TryResolve(this, out ent, _log.GetSawmill(Sawmill));

    /// <summary>
    /// Fetches an id from all existing <see cref="PersistentIdentifierComponent"/>. Attempts to add valid ids to an existing <see cref="PersistentIdRegisterComponent"/> 
    /// </summary>
    /// <param name="id">The desired id.</param>
    /// <param name="ent">The output variable storing the retrieved entity.</param>
    /// <param name="conditional">A conditional function applied to the search.</param>
    /// <param name="registry">An optional registry to register valid ids to when found. Improves speed of future searches.</param>
    /// <returns></returns>
    public bool TryFetchId(
        string id,
        out Entity<PersistentIdentifierComponent> ent,
        Func<Entity<PersistentIdentifierComponent>, bool>? conditional = null,
        PersistentIdRegisterComponent? registry = null,
        bool sendLogs = true)
    {
        ent = default!;
        conditional ??= _ => true;

        if (sendLogs) _log.GetSawmill(Sawmill).Info($"Attempting to fetch persistent id: {id}");

        var lookup = EntityQueryEnumerator<PersistentIdentifierComponent>();

        while (lookup.MoveNext(out var uid, out var idComp))
        {
            if (idComp.Id == id && conditional((uid, idComp)))
            {
                ent = (uid, idComp);
                if (registry is not null) registry.TryRegister(ent, _entMan);
                if (sendLogs) _log.GetSawmill(Sawmill).Info($"Entity found: {uid}");
                return true;
            }
        }

        if (sendLogs) _log.GetSawmill(Sawmill).Warning($"Unable to find entity with matching pid.");
        return false;
    }

    private void OnRegisterIdChange(EntityUid uid, PersistentIdRegisterComponent register, ref PersistentIdChangedEvent args)
    {
        // Culling will remove the existing reference as the new ID will not match that stored in the key.
        register.CullStaleEntities(_entMan);

        if (IsEmptyId(args.NewId) || args.NewId == args.OldId || args.Behaviour == PersistentIdChangeBehaviour.Sever)
            return; // Nothing more to do.

        if (!TryComp<PersistentIdentifierComponent>(args.Uid, out var idComp))
            return; // What would this even mean...?

        register.TryRegister((args.Uid, idComp), _entMan);
    }

    private bool IsEmptyId(string id) => id == EmptyId;

    public bool CompareId(PersistentEntityReference reference, string id)
        => !IsEmptyId(id) && reference.TargetId == id;
    public bool CompareId(PersistentEntityReference reference, Entity<PersistentIdentifierComponent> ent)
        => ent.Comp.IdInit && reference.TargetId == ent.Comp.Id;
    public bool CompareId(PersistentEntityReference reference, EntityUid uid)
    {
        var id = EnsureId(uid);
        return CompareId(reference, id);
    }

    public void AssignIdReference(ref PersistentEntityReference reference, string id)
    {
        _log.GetSawmill(Sawmill).Info($"Assigning id ({id}) to entity reference.");
        reference.TargetId = id;
    }
    public void AssignIdReference(ref PersistentEntityReference reference, Entity<PersistentIdentifierComponent> ent) {
        _log.GetSawmill(Sawmill).Info($"Assigning id from {ToPrettyString(ent)}");
        AssignIdReference(ref reference, ent.Comp.Id);
    }
    public void AssignIdReference(ref PersistentEntityReference reference, EntityUid uid)
    {
        var id = EnsureId(uid);
        AssignIdReference(ref reference, id);
    }
}