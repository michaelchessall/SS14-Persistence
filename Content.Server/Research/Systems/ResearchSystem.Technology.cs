using Content.Shared._Persistence14.Research.RecipeRelay;
using Content.Shared.Database;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    public bool UnlockTechnology(EntityUid client,
        string prototypeid,
        EntityUid user,
        ResearchClientComponent? component = null)
    {
        if (!ProtoMan.TryIndex<TechnologyPrototype>(prototypeid, out var prototype))
            return false;

        return UnlockTechnology(client, prototype, user, component);
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    public bool UnlockTechnology(EntityUid client,
        TechnologyPrototype prototype,
        EntityUid user,
        ResearchClientComponent? component = null)
    {
        if (!Resolve(client, ref component, false))
            return false;

        if (!TryGetClientServer((client, component), out var server))
            return false;
        var (serverUid, serverComp, dbComp) = server;

        if (!CanServerUnlockTechnology(client, prototype, out var cost, component, serverComp, dbComp))
            return false;

        AddTechnology(server, prototype);
        ModifyServerPoints(server, -cost, serverComp);
        UpdateTechnologyCards(server);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} unlocked {prototype.ID} (discipline: {prototype.Discipline}, tier: {prototype.Tier}) at {ToPrettyString(client)}, for server {ToPrettyString(server.Owner)}.");
        return true;
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    [PublicAPI]
    public void AddTechnology(EntityUid uid, string technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!ProtoMan.TryIndex<TechnologyPrototype>(technology, out var prototype))
            return;
        AddTechnology(uid, prototype, component);
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    public void AddTechnology(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? component = null, RecipeContainerComponent? container = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(uid, ref container))
            return;

        //todo this needs to support some other stuff, too
        foreach (var generic in technology.GenericUnlocks)
        {
            if (generic.PurchaseEvent != null)
                RaiseLocalEvent(generic.PurchaseEvent);
        }

        if (!component.UnlockedTechnologies.ContainsKey(technology.Discipline))
            component.UnlockedTechnologies.Add(technology.Discipline, new());
        if (!component.UnlockedTechnologies[technology.Discipline].Contains(technology.ID))
            component.UnlockedTechnologies[technology.Discipline].Add(technology.ID);

        var addedRecipes = new List<string>();
        foreach (var (unlock, qty) in technology.RecipeUnlocks)
        {
            ProtoMan.Resolve(unlock, out var recipeProto);
            if (recipeProto == null) continue;
            if (container.UnlockedRecipes.ContainsKey(unlock))
            {
                container.UnlockedRecipes[unlock] = container.UnlockedRecipes[unlock] + qty;
            }
            else
            {
                container.UnlockedRecipes.Add(unlock, qty);
            }
            addedRecipes.Add(unlock);
        }
        Dirty(uid, component);

        var ev = new TechnologyDatabaseModifiedEvent(addedRecipes);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    ///     Returns whether a technology can be unlocked on this database,
    ///     taking parent technologies into account.
    /// </summary>
    /// <returns>Whether it could be unlocked or not</returns>
    public bool CanServerUnlockTechnology(EntityUid uid,
        TechnologyPrototype technology,
        out int cost,
        ResearchClientComponent? client = null,
        ResearchServerComponent? serverComp = null,
        TechnologyDatabaseComponent? dbComp = null)
    {
        cost = 0;
        if (!Resolve(uid, ref client, false))
            return false;

        if (serverComp == null || dbComp == null)
        {
            if (!TryGetClientServer((uid, client), out var server))
                return false;
            serverComp = server.Comp1;
            dbComp = server.Comp2;
        }

        if (!IsTechnologyAvailable(dbComp, technology))
            return false;

        cost = (int)MathF.Floor(technology.Cost * CalculateDiversityMultiplier(serverComp, dbComp));
        if (cost > serverComp.Points)
            return false;

        return true;
    }

    private void OnDatabaseRegistrationChanged(EntityUid uid, TechnologyDatabaseComponent component, ref ResearchRegistrationChangedEvent args)
    {
        if (args.Server != null)
            return;

        component.CurrentTechnologyCards = new List<ProtoId<TechnologyPrototype>>();
        component.SupportedDisciplines = new List<ProtoId<TechDisciplinePrototype>>();
        component.UnlockedTechnologies = new Dictionary<ProtoId<TechDisciplinePrototype>, List<ProtoId<TechnologyPrototype>>>();
        Dirty(uid, component);
    }

    public IEnumerable<ProtoId<TechnologyPrototype>> GetUnlockedTechnologies(Entity<TechnologyDatabaseComponent> database)
    {
        foreach (var (_, list) in database.Comp.UnlockedTechnologies)
            foreach (var tech in list)
                yield return tech;
    }
}
