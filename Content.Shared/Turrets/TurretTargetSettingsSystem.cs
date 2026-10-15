using Content.Shared.Access.Systems;
using JetBrains.Annotations;
using Content.Shared.Station;

namespace Content.Shared.Turrets;

/// <summary>
/// This system is used for validating potential targets for NPCs with a <see cref="TurretTargetSettingsComponent"/> (i.e., turrets).
/// A turret will consider an entity a valid target if the entity does not possess any access assignments or names which appear on the
/// turret's <see cref="TurretTargetSettingsComponent.ExemptAccessAssignments"/> list.
/// </summary>
public sealed partial class TurretTargetSettingsSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    /// <summary>
    /// Adds or removes access assignments from a <see cref="TurretTargetSettingsComponent.ExemptAccessAssignments"/> list.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The assignment ID or personal name for acces</param>
    /// <param name="enabled">Set 'true' to add the exemption, or 'false' to remove it</param>
    /// <param name="dirty">Set 'true' to dirty the component</param>
    [PublicAPI]
    public void SetAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, int exemption, bool enabled, bool dirty = true)
    {
        if (enabled)
            ent.Comp.ExemptAccessAssignments.Add(exemption);
        else
            ent.Comp.ExemptAccessAssignments.Remove(exemption);

        if (dirty)
            Dirty(ent);
    }

    /// <summary>
    /// Adds or removes a collection of access levels from a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The collection of access level proto IDs to add or remove</param>
    /// <param name="enabled">Set 'true' to add the collection as exemptions, or 'false' to remove them</param>
    [PublicAPI]
    public void SetAccessLevelExemptions(Entity<TurretTargetSettingsComponent> ent, ICollection<int> exemptions, bool enabled)
    {
        foreach (var exemption in exemptions)
            SetAccessLevelExemption(ent, exemption, enabled, false);

        Dirty(ent);
    }

    /// <summary>
    /// Sets a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list to contain only a supplied collection of access levels.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemptions">The supplied collection of access level proto IDs</param>
    [PublicAPI]
    public void SyncAccessLevelExemptions(Entity<TurretTargetSettingsComponent> ent, ICollection<int> exemptions)
    {
        ent.Comp.ExemptAccessAssignments.Clear();
        SetAccessLevelExemptions(ent, exemptions, true);
    }

    /// <summary>
    /// Sets a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list to match that of another.
    /// </summary>
    /// <param name="target">The entity this is having its exemption list updated <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="source">The entity that is being used as a template for the target</param>
    [PublicAPI]
    public void SyncAccessLevelExemptions(Entity<TurretTargetSettingsComponent> target, Entity<TurretTargetSettingsComponent> source)
    {
        SyncAccessLevelExemptions(target, source.Comp.ExemptAccessAssignments);
    }

    /// <summary>
    /// Returns whether a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list contains a specific access level.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The access level proto ID being checked</param>
    [PublicAPI]
    public bool HasAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, int exemption)
    {
        if (ent.Comp.ExemptAccessAssignments.Count == 0)
            return false;

        return ent.Comp.ExemptAccessAssignments.Contains(exemption);
    }

    /// <summary>
    /// Returns whether a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list contains one or more access levels from another collection.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemptions"></param>
    [PublicAPI]
    public bool HasAnyAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, ICollection<int> exemptions)
    {
        if (ent.Comp.ExemptAccessAssignments.Count == 0)
            return false;

        foreach (var exemption in exemptions)
        {
            if (HasAccessLevelExemption(ent, exemption))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns whether an entity is a valid target for a turret.
    /// </summary>
    /// <remarks>
    /// Returns false if the target possesses one or more access tags that are present on the entity's <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list.
    /// </remarks>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="target">The target entity</param>
    [PublicAPI]
    public bool EntityIsTargetForTurret(Entity<TurretTargetSettingsComponent> ent, EntityUid target)
    {
        var owningStationUID = _station.GetOwningStation(ent);
        ICollection<int> accessAssignments =  new HashSet<int>();

        if (owningStationUID != null)
        {
            accessAssignments = _accessReader.FindAccessTagsPersistent(target, owningStationUID.Value);
        }

        return !HasAnyAccessLevelExemption(ent, accessAssignments);
    }
}
