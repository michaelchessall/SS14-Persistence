using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.PersistentIdentifier.Reference;
using Content.Shared.Teleportation.Components;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
///     Handles symmetrically linking two entities together, and removing links properly.
///     This does not do anything on its own (outside of deleting entities that have 0 links, if that option is true)
///     Systems can do whatever they please with the linked entities, such as <see cref="SharedPortalSystem"/>.
/// </summary>
public sealed partial class LinkedEntitySystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private PersistentIdentifierSystem _pid = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LinkedEntityComponent, ComponentShutdown>(OnLinkShutdown);
    }

    private void OnLinkShutdown(EntityUid uid, LinkedEntityComponent component, ComponentShutdown args)
    {
        // Remove any links to this entity when deleted.
        foreach (var entPid in component.LinkedEntities.ToArray())
        {
            if (!_pid.TryResolveId(entPid, out var ent))
                component.LinkedEntities.Remove(entPid);

            if (!Deleted(ent) && LifeStage(ent) < EntityLifeStage.Terminating && TryComp<LinkedEntityComponent>(ent, out var link))
            {
                TrySever((ent, link), uid);
            }
        }
    }

    #region Public API

    /// <summary>
    ///     Links two entities together. Does not require the existence of <see cref="LinkedEntityComponent"/> on either
    ///     already. Linking is symmetrical, so order doesn't matter.
    /// </summary>
    /// <param name="first">The first entity to link</param>
    /// <param name="second">The second entity to link</param>
    /// <param name="deleteOnEmptyLinks">Whether both entities should now delete once their links are removed</param>
    /// <returns>Whether linking was successful (e.g. they weren't already linked)</returns>
    public bool TryLink(EntityUid first, EntityUid second, bool deleteOnEmptyLinks = false)
    {
        var firstLink = EnsureComp<LinkedEntityComponent>(first);
        var secondLink = EnsureComp<LinkedEntityComponent>(second);

        var firstPid = _pid.EnsureId(first);
        var secondPid = _pid.EnsureId(second);

        firstLink.DeleteOnEmptyLinks = deleteOnEmptyLinks;
        secondLink.DeleteOnEmptyLinks = deleteOnEmptyLinks;

        _appearance.SetData(first, LinkedEntityVisuals.HasAnyLinks, true);
        _appearance.SetData(second, LinkedEntityVisuals.HasAnyLinks, true);

        Dirty(first, firstLink);
        Dirty(second, secondLink);

        if (firstLink.LinkedEntities.Add(secondPid) && secondLink.LinkedEntities.Add(firstPid))
        {
            var firstEv = new LinkedEntityChangedEvent(firstLink.LinkedEntities);
            RaiseLocalEvent(first, ref firstEv);
            var secondEv = new LinkedEntityChangedEvent(secondLink.LinkedEntities);
            RaiseLocalEvent(second, ref secondEv);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Does a one-way link from source to target.
    /// </summary>
    /// <param name="deleteOnEmptyLinks">Whether both entities should now delete once their links are removed</param>
    public bool OneWayLink(EntityUid source, EntityUid target, bool deleteOnEmptyLinks = false)
    {
        var firstLink = EnsureComp<LinkedEntityComponent>(source);
        firstLink.DeleteOnEmptyLinks = deleteOnEmptyLinks;

        _appearance.SetData(source, LinkedEntityVisuals.HasAnyLinks, true);

        Dirty(source, firstLink);

        if (firstLink.LinkedEntities.Add(_pid.EnsureId(target)))
        {
            var ev = new LinkedEntityChangedEvent(firstLink.LinkedEntities);
            RaiseLocalEvent(source, ref ev);
            return true;
        }

        return false;

    }

    /// <summary>
    ///     Unlinks two entities. Deletes either entity if <see cref="LinkedEntityComponent.DeleteOnEmptyLinks"/>
    ///     was true and its links are now empty. Symmetrical, so order doesn't matter.
    /// </summary>
    /// <param name="first">The first entity to unlink</param>
    /// <param name="second">The second entity to unlink</param>
    /// <param name="firstLink">Resolve comp</param>
    /// <param name="secondLink">Resolve comp</param>
    /// <returns>Whether unlinking was successful (e.g. they both were actually linked to one another)</returns>
    public bool TryUnlink(EntityUid first, EntityUid second,
        LinkedEntityComponent? firstLink = null, LinkedEntityComponent? secondLink = null)
    {
        if (!Resolve(first, ref firstLink))
            return false;

        if (!Resolve(second, ref secondLink))
            return false;

        var success = _pid.TryGetId(first, out var firstId) && firstLink.LinkedEntities.Remove(firstId) &&
                      _pid.TryGetId(second, out var secondId) && secondLink.LinkedEntities.Remove(secondId);

        _appearance.SetData(first, LinkedEntityVisuals.HasAnyLinks, firstLink.LinkedEntities.Any());
        _appearance.SetData(second, LinkedEntityVisuals.HasAnyLinks, secondLink.LinkedEntities.Any());

        Dirty(first, firstLink);
        Dirty(second, secondLink);

        var firstEv = new LinkedEntityChangedEvent(firstLink.LinkedEntities);
        RaiseLocalEvent(first, ref firstEv);
        var secondEv = new LinkedEntityChangedEvent(secondLink.LinkedEntities);
        RaiseLocalEvent(second, ref secondEv);

        if (firstLink.LinkedEntities.Count == 0 && firstLink.DeleteOnEmptyLinks)
            QueueDel(first);

        if (secondLink.LinkedEntities.Count == 0 && secondLink.DeleteOnEmptyLinks)
            QueueDel(second);

        return success;
    }

    /// <summary>
    /// Severs the connections of a given target node in a one-sided way. In general, you should really use TryUnlink.
    /// Raises no events on any of the removed connections, only rases events on the target.
    /// </summary>
    public bool TrySever(Entity<LinkedEntityComponent?> target, params EntityUid[] connectionsToRemove)
    {
        if (!Resolve(target, ref target.Comp))
            return false;

        if (connectionsToRemove.Length <= 0)
        {
            connectionsToRemove = target.Comp.LinkedEntities.Select(x =>
            {
                if (!_pid.TryResolveId(x, out var val))
                    return default;
                return val.Owner;
            }).ToArray();
        }

        var any = false;
        foreach (var connection in connectionsToRemove)
        {
            if (!_pid.TryGetId(connection, out var pid) || !target.Comp.LinkedEntities.Remove(pid))
                continue;

            Dirty(target);
        }

        if (!any)
            return false;

        var ev = new LinkedEntityChangedEvent(target.Comp.LinkedEntities);
        RaiseLocalEvent(target, ref ev);

        if (target.Comp.LinkedEntities.Count == 0 && target.Comp.DeleteOnEmptyLinks)
            QueueDel(target);

        return true;
    }

    public bool TrySever(Entity<LinkedEntityComponent?> target, params PersistentEntityReference[] idsToRemove)
    {
        if (!Resolve(target, ref target.Comp))
            return false;

        if (idsToRemove.Length <= 0)
            idsToRemove = target.Comp.LinkedEntities.ToArray();

        var any = false;
        foreach (var id in idsToRemove)
        {
            if (!target.Comp.LinkedEntities.Remove(id))
                continue;

            Dirty(target);
        }

        if (!any)
            return false;

        var ev = new LinkedEntityChangedEvent(target.Comp.LinkedEntities);
        RaiseLocalEvent(target, ref ev);

        if (target.Comp.LinkedEntities.Count == 0 && target.Comp.DeleteOnEmptyLinks)
            QueueDel(target);

        return true;
    }

    /// <summary>
    /// Get the first entity this entity is linked to.
    /// If multiple are linked only the first one is picked.
    /// </summary>
    public bool GetLink(EntityUid uid, [NotNullWhen(true)] out PersistentEntityReference? dest, LinkedEntityComponent? comp = null)
    {
        dest = null;
        if (!Resolve(uid, ref comp, false))
            return false;

        var first = comp.LinkedEntities.FirstOrDefault();
        if (first != default)
        {
            dest = first;
            return true;
        }

        return false;
    }

    #endregion
}

[ByRefEvent]
public readonly record struct LinkedEntityChangedEvent(HashSet<PersistentEntityReference> NewLinks);
