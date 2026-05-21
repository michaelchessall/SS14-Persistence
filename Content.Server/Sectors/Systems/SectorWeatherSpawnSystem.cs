using Content.Server.Power.Components;
using Content.Server.Sectors.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Shared.Sectors;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Sectors.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Sectors.Systems;

/// <summary>
/// Handles entity spawning and distance-based despawn rules driven by active sector weather.
/// </summary>
public sealed class SectorWeatherSpawnSystem : EntitySystem
{
    private const float UpdateInterval = 1f;
    private const int CoordinateAttemptsPerSpawnAttempt = 8;
    private const int MaxValidationFailuresBeforeEnd = 10;
    private const string SingularityPrototypeId = "Singularity";
    private const string WhiteHolePrototypeId = "WhiteHole";

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSectorSystem _sectors = default!;
    [Dependency] private readonly SectorWeatherSystem _sectorWeather = default!;
    [Dependency] private readonly SingularitySystem _singularity = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<(SpaceSector Sector, string WeatherId, int SpawnIndex), float> _spawnCooldowns = new();
    private readonly Dictionary<(SpaceSector Sector, string WeatherId, int SpawnIndex), int> _validationFailureCounts = new();
    private readonly List<MapCoordinates> _sectorSpawnOrigins = new();
    private readonly HashSet<(SpaceSector Sector, string WeatherId, int SpawnIndex)> _validSpawnKeys = new();
    private readonly List<(SpaceSector Sector, string WeatherId, int SpawnIndex)> _staleSpawnKeys = new();

    private float _updateTimer;

    public bool BypassCooldown(SpaceSector sector, string weatherId)
    {
        if (!_prototype.TryIndex<SectorWeatherPrototype>(weatherId, out var weather))
            return false;

        if (weather.Spawns.Count == 0)
            return false;

        for (var i = 0; i < weather.Spawns.Count; i++)
        {
            var key = (sector, weatherId, i);
            _spawnCooldowns[key] = 0f;
            _validationFailureCounts.Remove(key);
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer < UpdateInterval)
            return;

        var stepTime = _updateTimer;
        _updateTimer = 0f;

        ProcessSpawns(stepTime);
        ProcessDistanceDespawns();
    }

    private void ProcessSpawns(float stepTime)
    {
        _validSpawnKeys.Clear();
        var activeWeather = _sectorWeather.GetWeatherSnapshot();

        foreach (var (sector, weatherId) in activeWeather)
        {
            if (!_prototype.TryIndex<SectorWeatherPrototype>(weatherId, out var weather))
                continue;

            for (var i = 0; i < weather.Spawns.Count; i++)
            {
                var spawn = weather.Spawns[i];
                var key = (sector, weatherId, i);
                _validSpawnKeys.Add(key);

                var remaining = 0f;
                if (_spawnCooldowns.TryGetValue(key, out var cooldown))
                    remaining = cooldown - stepTime;

                if (remaining > 0f)
                {
                    _spawnCooldowns[key] = remaining;
                    continue;
                }

                // Consume this spawn window and start the next cooldown from YAML-defined interval.
                _spawnCooldowns[key] = MathF.Max(spawn.SpawnInterval, 0.1f);

                var configuredAttempts = Math.Max(spawn.SpawnAttempts, 1);
                var sectorCount = CountEntitiesInSectorByPrototype(spawn.Prototype, sector);
                var activeCount = spawn.MaxActive > 0
                    ? CountSpawnedEntities(weatherId, spawn.Prototype, sector)
                    : 0;

                var spawnedAny = false;
                var hadValidationFailure = false;

                for (var attempt = 0; attempt < configuredAttempts; attempt++)
                {
                    if (spawn.SectorMaxCount > 0 && sectorCount >= spawn.SectorMaxCount)
                        break;

                    if (spawn.MaxActive > 0 && activeCount >= spawn.MaxActive)
                        break;

                    if (spawn.SectorMinCount > 0 && sectorCount >= spawn.SectorMinCount)
                    {
                        var chance = Math.Clamp(spawn.SpawnChanceAboveSectorMin, 0f, 1f);
                        if (chance <= 0f || !_random.Prob(chance))
                            continue;
                    }

                    if (!TrySpawnInSector(sector, weatherId, spawn))
                    {
                        hadValidationFailure = true;
                        continue;
                    }

                    spawnedAny = true;
                    sectorCount++;
                    if (spawn.MaxActive > 0)
                        activeCount++;
                }

                if (spawnedAny)
                {
                    _validationFailureCounts.Remove(key);
                    continue;
                }

                if (!hadValidationFailure)
                    continue;

                var failures = 0;
                if (_validationFailureCounts.TryGetValue(key, out var existingFailures))
                    failures = existingFailures;

                failures++;
                if (failures >= MaxValidationFailuresBeforeEnd)
                {
                    _validationFailureCounts.Remove(key);
                }
                else
                {
                    _validationFailureCounts[key] = failures;
                }
            }
        }

        _staleSpawnKeys.Clear();
        foreach (var key in _spawnCooldowns.Keys)
        {
            if (!_validSpawnKeys.Contains(key))
                _staleSpawnKeys.Add(key);
        }

        foreach (var key in _staleSpawnKeys)
        {
            _spawnCooldowns.Remove(key);
        }

        _staleSpawnKeys.Clear();
        foreach (var key in _validationFailureCounts.Keys)
        {
            if (!_validSpawnKeys.Contains(key))
                _staleSpawnKeys.Add(key);
        }

        foreach (var key in _staleSpawnKeys)
        {
            _validationFailureCounts.Remove(key);
        }
    }

    private bool TrySpawnInSector(SpaceSector sector, string weatherId, SectorWeatherSpawnEntry spawn)
    {
        for (var i = 0; i < CoordinateAttemptsPerSpawnAttempt; i++)
        {
            if (!TryGetSpawnCoordinates(sector, spawn, out var coords))
                continue;

            if (!CanSpawnAt(sector, coords, spawn))
                continue;

            if (spawn.Prototype.Id == SingularityPrototypeId)
            {
                // White holes mirror singularity coordinates; if the mirrored position is invalid,
                // reject this singularity spawn attempt entirely.
                var whiteHoleCoords = new MapCoordinates(-coords.Position, coords.MapId);
                var whiteHoleSector = _sectors.GetSector(whiteHoleCoords.Position);

                if (!CanSpawnAt(whiteHoleSector, whiteHoleCoords, spawn, WhiteHolePrototypeId, checkSamePrototypeDistance: false))
                    continue;
            }

            var spawned = Spawn(spawn.Prototype, coords);
            var marker = EnsureComp<SectorWeatherSpawnedComponent>(spawned);
            marker.SpawnWorldPosition = _transform.GetWorldPosition(spawned);
            marker.MaxDistanceFromSpawn = spawn.DespawnDistanceFromSpawn ?? 0f;
            marker.WeatherId = weatherId;
            marker.SpawnPrototype = spawn.Prototype;

            if (spawn.Prototype.Id == SingularityPrototypeId)
                ApplySingularitySpawnLevel(spawned, spawn);

            return true;
        }

        return false;
    }

    private void ApplySingularitySpawnLevel(EntityUid singularity, SectorWeatherSpawnEntry spawn)
    {
        if (spawn.MinSpawnLevel is null && spawn.MaxSpawnLevel is null)
            return;

        var minLevel = (int) (spawn.MinSpawnLevel ?? 1);
        var maxLevel = (int) (spawn.MaxSpawnLevel ?? spawn.MinSpawnLevel ?? 1);

        minLevel = Math.Clamp(minLevel, 1, SharedSingularitySystem.MaxSingularityLevel);
        maxLevel = Math.Clamp(maxLevel, 1, SharedSingularitySystem.MaxSingularityLevel);

        if (maxLevel < minLevel)
            (minLevel, maxLevel) = (maxLevel, minLevel);

        var chosenLevel = _random.Next(minLevel, maxLevel + 1);
        _singularity.SetEnergy(singularity, GetEnergyForSingularityLevel(chosenLevel));
    }

    private static float GetEnergyForSingularityLevel(int level)
    {
        return level switch
        {
            >= 6 => 5000f,
            5 => 2000f,
            4 => 1000f,
            3 => 500f,
            2 => 200f,
            _ => 1f,
        };
    }

    private bool TryGetSpawnCoordinates(SpaceSector sector, SectorWeatherSpawnEntry spawn, out MapCoordinates coordinates)
    {
        _sectorSpawnOrigins.Clear();

        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { Valid: true } attached || TerminatingOrDeleted(attached))
                continue;

            if (_sectors.GetSector(attached) != sector)
                continue;

            var mapCoords = _transform.GetMapCoordinates(attached);
            if (mapCoords.MapId == MapId.Nullspace)
                continue;

            _sectorSpawnOrigins.Add(mapCoords);
        }

        if (_sectorSpawnOrigins.Count == 0)
        {
            coordinates = default;
            return false;
        }

        var origin = _random.Pick(_sectorSpawnOrigins);
        var minRange = MathF.Max(spawn.MinSpawnRange, 0f);
        var maxRange = MathF.Max(spawn.MaxSpawnRange, minRange);
        for (var i = 0; i < CoordinateAttemptsPerSpawnAttempt; i++)
        {
            var distance = _random.NextFloat(minRange, maxRange);
            var offset = _random.NextAngle().ToVec() * distance;
            var position = origin.Position + offset;

            if (_sectors.GetSector(position) != sector)
                continue;

            coordinates = new MapCoordinates(position, origin.MapId);
            return true;
        }

        coordinates = default;
        return false;
    }

    private bool CanSpawnAt(
        SpaceSector sector,
        MapCoordinates coordinates,
        SectorWeatherSpawnEntry spawn,
        string? samePrototypeId = null,
        bool checkSamePrototypeDistance = true)
    {
        samePrototypeId ??= spawn.Prototype.Id;

        if (spawn.MinDistanceFromPoweredEntity > 0f)
        {
            foreach (var (_, receiver) in _lookup.GetEntitiesInRange<ApcPowerReceiverComponent>(coordinates, spawn.MinDistanceFromPoweredEntity))
            {
                if (receiver.Powered)
                    return false;
            }
        }

        if (checkSamePrototypeDistance && spawn.MinDistanceFromSamePrototype > 0f)
        {
            foreach (var (uid, metadata) in _lookup.GetEntitiesInRange<MetaDataComponent>(coordinates, spawn.MinDistanceFromSamePrototype))
            {
                if (metadata.EntityPrototype is not { } entityPrototype)
                    continue;

                if (entityPrototype.ID != samePrototypeId)
                    continue;

                if (_sectors.GetSector(uid) != sector)
                    continue;

                return false;
            }
        }

        return true;
    }

    private int CountEntitiesInSectorByPrototype(EntProtoId prototype, SpaceSector sector)
    {
        var query = EntityQueryEnumerator<MetaDataComponent, TransformComponent>();
        var count = 0;

        while (query.MoveNext(out var uid, out var metadata, out _))
        {
            if (metadata.EntityPrototype is not { } entityPrototype)
                continue;

            if (entityPrototype.ID != prototype.Id)
                continue;

            if (_sectors.GetSector(uid) != sector)
                continue;

            count++;
        }

        return count;
    }

    private void ProcessDistanceDespawns()
    {
        var query = EntityQueryEnumerator<SectorWeatherSpawnedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.MaxDistanceFromSpawn <= 0f)
                continue;

            var maxDistanceSquared = comp.MaxDistanceFromSpawn * comp.MaxDistanceFromSpawn;
            var currentWorldPos = _transform.GetWorldPosition(uid);
            if ((currentWorldPos - comp.SpawnWorldPosition).LengthSquared() <= maxDistanceSquared)
                continue;

            QueueDel(uid);
        }
    }

    private int CountSpawnedEntities(string weatherId, EntProtoId prototype, SpaceSector sector)
    {
        var query = EntityQueryEnumerator<SectorWeatherSpawnedComponent>();
        var count = 0;

        while (query.MoveNext(out _, out var comp))
        {
            if (comp.WeatherId != weatherId || comp.SpawnPrototype != prototype)
                continue;

            if (_sectors.GetSector(comp.SpawnWorldPosition) != sector)
                continue;

            count++;
        }

        return count;
    }
}
