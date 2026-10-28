using System.Linq;
using System.Numerics;
using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.Research.RecipeRelay;
using Content.Shared.Lathe;
using Content.Shared.Pinpointer;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Station;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Systems;

public abstract partial class SharedResearchSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedLatheSystem _lathe = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private PersistentIdentifierSystem _pid = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechnologyDatabaseComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, TechnologyDatabaseComponent component, MapInitEvent args)
    {
        UpdateTechnologyCards(uid, component);
    }

    public void UpdateTechnologyCards(EntityUid uid, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var availableTechnology = GetAvailableTechnologies(uid, component);

        component.CurrentTechnologyCards = availableTechnology
            .Select(p => (ProtoId<TechnologyPrototype>)p.ID)
            .ToList();
        Dirty(uid, component);
    }

    public List<TechnologyPrototype> GetAvailableTechnologies(EntityUid uid, TechnologyDatabaseComponent? db = null)
    {
        if (db is null)
        {
            if (!TryGetClientServer(uid, out var server))
                return new List<TechnologyPrototype>();
            db = server.Comp2;
        }
        else if (!Resolve(uid, ref db))
            return new List<TechnologyPrototype>();

        var availableTechnologies = new List<TechnologyPrototype>();
        var disciplineTiers = GetDisciplineTiers(db);
        foreach (var tech in ProtoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (IsTechnologyAvailable(db, tech, disciplineTiers))
                availableTechnologies.Add(tech);
        }

        return availableTechnologies
            .OrderBy(p => p.Discipline)
            .ThenBy(p => p.Tier)
            .ThenBy(p => p.Name)
            .ToList();
    }

    public bool IsTechnologyAvailable(TechnologyDatabaseComponent component, TechnologyPrototype tech, Dictionary<string, int>? disciplineTiers = null)
    {
        disciplineTiers ??= GetDisciplineTiers(component);

        if (tech.Hidden)
            return false;

        if (!component.SupportedDisciplines.Contains(tech.Discipline))
            return false;

        if (tech.Tier > disciplineTiers[tech.Discipline])
            return false;

        //       if (component.UnlockedTechnologies.Contains(tech.ID))
        //           return false;

        foreach (var prereq in tech.TechnologyPrerequisites)
        {
            var has = false;
            foreach (var list in component.UnlockedTechnologies.Values)
                if (list.Contains(prereq))
                {
                    has = true;
                    break;
                }
            if (!has)
                return false;
        }

        return true;
    }

    public Dictionary<string, int> GetDisciplineTiers(TechnologyDatabaseComponent component)
    {
        var tiers = new Dictionary<string, int>();
        foreach (var discipline in component.SupportedDisciplines)
        {
            tiers.Add(discipline, GetHighestDisciplineTier(component, discipline));
        }

        return tiers;
    }

    public int GetHighestDisciplineTier(TechnologyDatabaseComponent component, string disciplineId)
    {
        return GetHighestDisciplineTier(component, ProtoMan.Index<TechDisciplinePrototype>(disciplineId));
    }

    public int GetHighestDisciplineTier(TechnologyDatabaseComponent component, TechDisciplinePrototype techDiscipline)
    {
        var allTech = ProtoMan.EnumeratePrototypes<TechnologyPrototype>()
            .Where(p => p.Discipline == techDiscipline.ID && !p.Hidden).ToList();
        var allUnlocked = new List<TechnologyPrototype>();
        foreach (var (disc, list) in component.UnlockedTechnologies)
        {
            foreach (var recipe in list)
            {
                var proto = ProtoMan.Index<TechnologyPrototype>(recipe);
                if (proto.Discipline != techDiscipline.ID)
                    continue;
                allUnlocked.Add(proto);
            }
        }

        var highestTier = techDiscipline.TierPrerequisites.Keys.Max();
        var tier = 2; //tier 1 is always given

        // todo this might break if you have hidden technologies. i'm not sure

        while (tier <= highestTier)
        {
            // we need to get the tech for the tier 1 below because that's
            // what the percentage in TierPrerequisites is referring to.
            var unlockedTierTech = allUnlocked.Where(p => p.Tier == tier - 1).ToList();
            var allTierTech = allTech.Where(p => p.Discipline == techDiscipline.ID && p.Tier == tier - 1).ToList();

            if (allTierTech.Count == 0)
                break;

            var percent = (float)unlockedTierTech.Count / allTierTech.Count;
            if (percent < techDiscipline.TierPrerequisites[tier])
                break;

            tier++;
        }

        return tier - 1;
    }

    public FormattedMessage GetTechnologyDescription(
        TechnologyPrototype technology,
        float costMultiplier,
        bool includeCost = true,
        bool includeTier = true,
        bool includePrereqs = false,
        TechDisciplinePrototype? disciplinePrototype = null)
    {
        var description = new FormattedMessage();
        if (includeTier)
        {
            disciplinePrototype ??= ProtoMan.Index(technology.Discipline);
            description.AddMarkupOrThrow(Loc.GetString("research-console-tier-discipline-info",
                ("tier", technology.Tier), ("color", disciplinePrototype.Color), ("discipline", Loc.GetString(disciplinePrototype.Name))));
            description.PushNewline();
        }

        if (includeCost)
        {
            description.AddMarkupOrThrow(Loc.GetString("research-console-cost", ("amount", MathF.Floor(technology.Cost * costMultiplier))));
            description.PushNewline();
        }

        if (includePrereqs && technology.TechnologyPrerequisites.Any())
        {
            description.AddMarkupOrThrow(Loc.GetString("research-console-prereqs-list-start"));
            foreach (var recipe in technology.TechnologyPrerequisites)
            {
                var techProto = ProtoMan.Index(recipe);
                description.PushNewline();
                description.AddMarkupOrThrow(Loc.GetString("research-console-prereqs-list-entry",
                    ("text", Loc.GetString(techProto.Name))));
            }
            description.PushNewline();
        }

        description.AddMarkupOrThrow(Loc.GetString("research-console-unlocks-list-start"));
        foreach (var recipe in technology.RecipeUnlocks.Keys)
        {
            var recipeProto = ProtoMan.Index(recipe);
            description.PushNewline();
            description.AddMarkupOrThrow(Loc.GetString("research-console-unlocks-list-entry",
                ("name", _lathe.GetRecipeName(recipeProto))));
        }
        foreach (var generic in technology.GenericUnlocks)
        {
            description.PushNewline();
            description.AddMarkupOrThrow(Loc.GetString("research-console-unlocks-list-entry-generic",
                ("text", Loc.GetString(generic.UnlockDescription))));
        }

        return description;
    }

    /// <summary>
    ///     Returns whether a technology is unlocked on this database or not.
    /// </summary>
    /// <returns>Whether it is unlocked or not</returns>
    public bool IsTechnologyUnlocked(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? component = null)
    {
        return Resolve(uid, ref component) && IsTechnologyUnlocked(uid, technology.ID, component);
    }

    /// <summary>
    ///     Returns whether a technology is unlocked on this database or not.
    /// </summary>
    /// <returns>Whether it is unlocked or not</returns>
    public bool IsTechnologyUnlocked(EntityUid uid, string technologyId, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        foreach (var list in component.UnlockedTechnologies.Values)
            if (list.Contains(technologyId))
                return true;

        return false;
    }

    /// <summary>
    /// Removes a technology and its recipes from a technology database.
    /// </summary>
    public bool TryRemoveTechnology(Entity<TechnologyDatabaseComponent> entity, ProtoId<TechnologyPrototype> tech)
    {
        return TryRemoveTechnology(entity, ProtoMan.Index(tech));
    }

    /// <summary>
    /// Removes a technology and its recipes from a technology database.
    /// </summary>
    [PublicAPI]
    public bool TryRemoveTechnology(Entity<TechnologyDatabaseComponent> entity, TechnologyPrototype tech)
    {
        if (!entity.Comp.UnlockedTechnologies.Remove(tech.ID))
            return false;

        // check to make sure we didn't somehow get the recipe from another tech.
        // unlikely, but whatever
        var recipes = tech.RecipeUnlocks;
        foreach (var (recipe, qty) in recipes)
        {
            foreach (var list in entity.Comp.UnlockedTechnologies.Values)
            {
                bool found = false;
                foreach (var unlockedTech in list)
                {
                    var unlockedTechProto = ProtoMan.Index<TechnologyPrototype>(unlockedTech);

                    if (!unlockedTechProto.RecipeUnlocks.ContainsKey(recipe))
                        continue;

                    found = true;
                    break;
                }
                if (found)
                    break;
            }
        }
        Dirty(entity, entity.Comp);
        UpdateTechnologyCards(entity, entity);
        return true;
    }

    /// <summary>
    /// Clear all unlocked technologies from the database.
    /// </summary>
    [PublicAPI]
    public void ClearTechs(EntityUid uid, TechnologyDatabaseComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.UnlockedTechnologies.Count == 0)
            return;

        comp.UnlockedTechnologies.Clear();
        Dirty(uid, comp);
    }

    /// <summary>
    /// Adds a lathe recipe to the specified technology database
    /// without checking if it can be unlocked.
    /// </summary>
    public void AddLatheRecipe(EntityUid uid, ProtoId<LatheRecipePrototype> recipe, int quanity, RecipeContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.UnlockedRecipes.ContainsKey(recipe))
        {
            component.UnlockedRecipes[recipe] = component.UnlockedRecipes[recipe] + quanity;
        }
        else component.UnlockedRecipes.Add(recipe, quanity);
        Dirty(uid, component);

        var ev = new TechnologyDatabaseModifiedEvent(new List<string> { recipe });
        RaiseLocalEvent(uid, ref ev);
    }

    public bool TryGetClientServer(Entity<ResearchClientComponent?> client, out Entity<ResearchServerComponent, TechnologyDatabaseComponent> server)
    {
        server = default!;
        if (!Resolve(client, ref client.Comp))
            return false;

        // Attempt to resolve existing connection.
        if (_pid.TryResolveId(client.Comp.Server, out var serverEnt))
        {
            if (TryComp<ResearchServerComponent>(serverEnt.Owner, out var serverComp) &&
                TryComp<TechnologyDatabaseComponent>(serverEnt.Owner, out var dbComp) &&
                ValidClientServer((client, client.Comp), (serverEnt.Owner, serverComp)))
            {
                server = (serverEnt.Owner, serverComp, dbComp);
                return true;
            }
        }

        // If existing connection fails, query to find new valid connection.
        var servers = GetCompatibleServers(client)
            .Select(candidate => (Server: candidate, Distance: Vector2.DistanceSquared(_transform.GetWorldPosition(client.Owner), _transform.GetWorldPosition(candidate.Owner))))
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Server.Owner).ToList();

        if (servers.Count == 0)
        {
            client.Comp.Server = PersistentIdentifierSystem.EmptyId;
            return false;
        }

        server = servers[0].Server;
        client.Comp.Server = _pid.EnsureId(server);
        return true;
    }

    protected bool ValidClientServer(Entity<ResearchClientComponent> client, Entity<ResearchServerComponent> server)
    {
        var clientXform = Transform(client.Owner);
        var serverXform = Transform(server.Owner);
        if (clientXform.GridUid == null ||
            clientXform.GridUid != serverXform.GridUid)
            return false;

        var clientFaction = _station.GetOwningStation(client.Owner);
        var serverFaction = _station.GetOwningStation(server.Owner);
        if (clientFaction == null ||
            clientFaction != serverFaction)
            return false;

        return true;
    }

    protected void SyncDB(Entity<TechnologyDatabaseComponent> source, Entity<TechnologyDatabaseComponent> target)
    {
        target.Comp.SupportedDisciplines = source.Comp.SupportedDisciplines;
        target.Comp.UnlockedTechnologies = source.Comp.UnlockedTechnologies;

        Dirty(target);
        UpdateTechnologyCards(target.Owner);
        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(target.Owner, ref ev);
    }

    protected void ResetDB(Entity<ResearchServerComponent, TechnologyDatabaseComponent> server)
    {
        var (serverUid, serverComp, dbComp) = server;
        dbComp.UnlockedTechnologies = [];

        Dirty(serverUid, dbComp);
        UpdateTechnologyCards(serverUid, dbComp);
        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(serverUid, ref ev);
    }

    public IEnumerable<Entity<ResearchServerComponent, TechnologyDatabaseComponent>> GetCompatibleServers(EntityUid uid)
    {
        var xForm = Transform(uid);
        if (xForm.GridUid is not { } grid)
            yield break; // Not on a grid, no compatible servers for you.

        var faction = _station.GetOwningStation(uid);

        var query = EntityQueryEnumerator<ResearchServerComponent, TechnologyDatabaseComponent>();
        while (query.MoveNext(out var serverUid, out var serverComp, out var dbComp))
        {
            var serverXform = Transform(serverUid);
            if (grid != serverXform.GridUid)
                continue;

            var serverFaction = _station.GetOwningStation(serverUid);
            if (faction != serverFaction)
                continue;

            yield return (serverUid, serverComp, dbComp);
        }
    }

    public bool ServerCompatible(EntityUid uid, Entity<ResearchServerComponent?> server)
    {
        var xformA = Transform(uid);
        var xformB = Transform(server.Owner);

        if (xformA.GridUid is not { } || xformA.GridUid != xformB.GridUid)
            return false;

        var factionA = _station.GetOwningStation(uid);
        var factionB = _station.GetOwningStation(server.Owner);
        if (factionA != factionB)
            return false;

        return true;
    }
}
