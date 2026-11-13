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
    /// <param name="exemption">The assignment ID for acces</param>
    /// <param name="enabled">Set 'true' to add the exemption, or 'false' to remove it</param>
    /// <param name="dirty">Set 'true' to dirty the component</param>
    [PublicAPI]
    public void SetAccessAssignment(Entity<TurretTargetSettingsComponent> ent, int exemption, bool enabled, bool dirty = true)
    {
        if (enabled)
            ent.Comp.ExemptAccessAssignments.Add(exemption);
        else
            ent.Comp.ExemptAccessAssignments.Remove(exemption);

        if (dirty)
            Dirty(ent);
    }

    /// <summary>
    /// Adds or removes a collection of access assignments from a <see cref="TurretTargetSettingsComponent.ExemptAccessAssignments"/> list.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The collection of access assignment IDs to add or remove</param>
    /// <param name="enabled">Set 'true' to add the collection as exemptions, or 'false' to remove them</param>
    [PublicAPI]
    public void SetAccessAssignments(Entity<TurretTargetSettingsComponent> ent, ICollection<int> exemptions, bool enabled)
    {
        foreach (var exemption in exemptions)
            SetAccessAssignment(ent, exemption, enabled, false);

        Dirty(ent);
    }

/// <summary>
    /// Adds or removes access assignments from a <see cref="TurretTargetSettingsComponent.ExemptAccessNames"/> list.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The  personal name for acces</param>
    /// <param name="enabled">Set 'true' to add the exemption, or 'false' to remove it</param>
    /// <param name="dirty">Set 'true' to dirty the component</param>
    /// <summary>
    [PublicAPI]
    public void SetAccessName(Entity<TurretTargetSettingsComponent> ent, string exemption, bool delete, bool dirty = true)
    {
        if (delete)
            ent.Comp.ExemptAccessNames.Add(exemption);
        else
            ent.Comp.ExemptAccessNames.Remove(exemption);

        if (dirty)
            Dirty(ent);
    }

    /// <summary>
    /// Adds or removes a collection of access names from a <see cref="TurretTargetSettingsComponent.ExemptAccessNames"/> list.
    /// </summary>
    /// <param name="ent">The entity and its <see personal names to add or remove</param>
    /// <param name="enabled">Set 'true' to add the collection as exemptions, or 'false' to remove them</param>
    [PublicAPI]
    public void SetAccessNames(Entity<TurretTargetSettingsComponent> ent, ICollection<string> exemptions, bool enabled)
    {
        foreach (var exemption in exemptions)
            SetAccessName(ent, exemption, enabled, false);

        Dirty(ent);
    }

    /// <summary>
    /// Sets a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list to contain only a supplied collection of access levels.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemptions">The supplied collection of access level proto IDs</param>
    [PublicAPI]
    public void SyncAccessAssignmentExemptions(Entity<TurretTargetSettingsComponent> ent, ICollection<int> exemptionAssignments)
    {
        ent.Comp.ExemptAccessAssignments.Clear();
        SetAccessAssignments(ent, exemptionAssignments, true);
    }
    [PublicAPI]
    public void SyncAccessNameExemptions(Entity<TurretTargetSettingsComponent> ent, ICollection<string> exemptionNames)
    {
        ent.Comp.ExemptAccessNames.Clear();
        SetAccessNames(ent, exemptionNames, true);
    }

    /// <summary>
    /// Sets a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list to match that of another.
    /// </summary>
    /// <param name="target">The entity this is having its exemption list updated <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="source">The entity that is being used as a template for the target</param>
    [PublicAPI]
    public void SyncAccessAssignmentExemptions(Entity<TurretTargetSettingsComponent> target, Entity<TurretTargetSettingsComponent> source)
    {
        SyncAccessAssignmentExemptions(target, source.Comp.ExemptAccessAssignments);
    }

    [PublicAPI]
    public void SyncAccessNameExemptions(Entity<TurretTargetSettingsComponent> target, Entity<TurretTargetSettingsComponent> source)
    {
        SyncAccessNameExemptions(target, source.Comp.ExemptAccessNames);
    }

    /// <summary>
    /// Returns whether a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list contains a specific access level.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The access level proto ID being checked</param>
    [PublicAPI]
    public bool HasAccessAssignmentExemption(Entity<TurretTargetSettingsComponent> ent, int exemptionAssignment)
    {
        if (ent.Comp.ExemptAccessAssignments.Count == 0)
            return false;

        return ent.Comp.ExemptAccessAssignments.Contains(exemptionAssignment);
    }

    /// <summary>
    /// Returns whether a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list contains a specific access level.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The access level proto ID being checked</param>
    [PublicAPI]
    public bool HasAccessNameExemption(Entity<TurretTargetSettingsComponent> ent, string exemptionName)
    {
        if (ent.Comp.ExemptAccessNames.Count == 0)
            return false;

        return ent.Comp.ExemptAccessNames.Contains(exemptionName);
    }

    /// <summary>
    /// Returns whether a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list contains one or more access levels from another collection.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemptions"></param>
    [PublicAPI]
    public bool HasAnyAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, ICollection<int> assignmentExemptions, ICollection<string> nameExemptions)
    {
        if (ent.Comp.ExemptAccessAssignments.Count == 0 && ent.Comp.ExemptAccessNames.Count == 0)
            return false;

        foreach (var exemption in assignmentExemptions)
        {
            if (HasAccessAssignmentExemption(ent, exemption))
                return true;
        }

        foreach (var exemption in nameExemptions)
        {
            if (HasAccessNameExemption(ent, exemption))
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
        ICollection<string> accessNames =  new HashSet<string>();

        if (owningStationUID != null)
        {
            accessAssignments = _accessReader.FindAccessTagsAssignments(target, owningStationUID.Value);
        }
        accessNames = _accessReader.FindAccessTagsNames(target);

        return !HasAnyAccessLevelExemption(ent, accessAssignments, accessNames);
    }
}
