using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Sectors.Components;
using Content.Server.Sectors.Events;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Sectors.Events;
using Content.Shared.Sectors;
using Content.Shared.Sectors.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Log;

namespace Content.Server.Sectors.Systems;

/// <summary>
/// Tracks active sector weather events and broadcasts changes for UI systems.
/// </summary>
public sealed class SectorWeatherSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    private readonly Dictionary<SpaceSector, string> _activeWeather = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        SubscribeLocalEvent<SectorWeatherPersistenceComponent, MapInitEvent>(OnPersistenceMapInit);
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        RestoreWeatherFromCvars();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        _activeWeather.Clear();
        BroadcastWeatherState();
    }

    public Dictionary<SpaceSector, string> GetWeatherSnapshot()
    {
        return new Dictionary<SpaceSector, string>(_activeWeather);
    }

    public Dictionary<SpaceSector, string> GetHazardWeatherSnapshot()
    {
        var snapshot = new Dictionary<SpaceSector, string>();

        foreach (var (sector, weatherId) in _activeWeather)
        {
            if (!_prototypes.TryIndex<SectorWeatherPrototype>(weatherId, out var weather))
                continue;

            if (weather.Hazard)
                snapshot[sector] = weatherId;
        }

        return snapshot;
    }

    public bool TrySetWeather(SpaceSector sector, string weatherId)
    {
        if (!_prototypes.HasIndex<SectorWeatherPrototype>(weatherId))
            return false;

        _activeWeather[sector] = weatherId;
        SetSectorWeatherCvar(sector, weatherId);
        SyncMapPersistenceState();
        RaiseLocalEvent(new SectorWeatherChangedEvent(sector, weatherId));
        BroadcastWeatherState();
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"Sector weather event '{weatherId}' set for sector {sector}.");
        return true;
    }

    public bool ClearWeather(SpaceSector sector)
    {
        if (!_activeWeather.Remove(sector, out var clearedId))
            return false;

        SetSectorWeatherCvar(sector, string.Empty);
        SyncMapPersistenceState();
        RaiseLocalEvent(new SectorWeatherChangedEvent(sector, null));
        BroadcastWeatherState();
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"Sector weather event '{clearedId}' cleared from sector {sector}.");
        return true;
    }

    public bool BypassCooldown(SpaceSector sector)
    {
        if (!_activeWeather.ContainsKey(sector))
        {
            Logger.WarningS("sector-weather", $"Attempted to bypass cooldown for sector {sector}, but no active weather found.");
            return false;
        }

        // Logic to bypass cooldowns (e.g., resetting timers or flags)
        Logger.InfoS("sector-weather", $"Cooldown bypassed for sector {sector}.");
        return true;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Connected)
            return;

        RaiseNetworkEvent(new SectorWeatherStateUpdateEvent(GetWeatherSnapshot()), args.Session.Channel);
    }

    private void BroadcastWeatherState()
    {
        RaiseNetworkEvent(new SectorWeatherStateUpdateEvent(GetWeatherSnapshot()));
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (args.New != GameRunLevel.InRound)
            return;

        ApplyPersistedWeatherFromLoadedMaps();
    }

    private void OnPersistenceMapInit(Entity<SectorWeatherPersistenceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ActiveWeather.Count == 0)
            return;

        ApplyPersistedWeatherSnapshot(ent.Comp.ActiveWeather);
    }

    private void RestoreWeatherFromCvars()
    {
        var restoredAny = false;

        foreach (var sector in Enum.GetValues<SpaceSector>())
        {
            var weatherId = _cfg.GetCVar(GetSectorWeatherCvar(sector));
            if (string.IsNullOrWhiteSpace(weatherId))
                continue;

            if (!_prototypes.HasIndex<SectorWeatherPrototype>(weatherId))
            {
                Logger.WarningS("sector-weather", $"Ignoring invalid persisted weather '{weatherId}' for sector {sector}.");
                SetSectorWeatherCvar(sector, string.Empty);
                continue;
            }

            _activeWeather[sector] = weatherId;
            RaiseLocalEvent(new SectorWeatherChangedEvent(sector, weatherId));
            restoredAny = true;
        }

        if (restoredAny)
            BroadcastWeatherState();
    }

    private void ApplyPersistedWeatherFromLoadedMaps()
    {
        var query = EntityQueryEnumerator<SectorWeatherPersistenceComponent, MapComponent>();
        while (query.MoveNext(out _, out var persistence, out _))
        {
            if (persistence.ActiveWeather.Count == 0)
                continue;

            if (SnapshotsMatch(persistence.ActiveWeather))
                return;

            ApplyPersistedWeatherSnapshot(persistence.ActiveWeather);
            return;
        }
    }

    private void ApplyPersistedWeatherSnapshot(Dictionary<SpaceSector, string> snapshot)
    {
        _activeWeather.Clear();

        foreach (var sector in Enum.GetValues<SpaceSector>())
        {
            if (!snapshot.TryGetValue(sector, out var weatherId) || string.IsNullOrWhiteSpace(weatherId))
            {
                SetSectorWeatherCvar(sector, string.Empty);
                continue;
            }

            if (!_prototypes.HasIndex<SectorWeatherPrototype>(weatherId))
            {
                Logger.WarningS("sector-weather", $"Ignoring invalid persisted weather '{weatherId}' for sector {sector}.");
                SetSectorWeatherCvar(sector, string.Empty);
                continue;
            }

            _activeWeather[sector] = weatherId;
            SetSectorWeatherCvar(sector, weatherId);
            RaiseLocalEvent(new SectorWeatherChangedEvent(sector, weatherId));
        }

        BroadcastWeatherState();
    }

    private bool SnapshotsMatch(Dictionary<SpaceSector, string> snapshot)
    {
        if (_activeWeather.Count != snapshot.Count)
            return false;

        foreach (var (sector, weatherId) in snapshot)
        {
            if (!_activeWeather.TryGetValue(sector, out var activeWeatherId) || activeWeatherId != weatherId)
                return false;
        }

        return true;
    }

    private void SyncMapPersistenceState()
    {
        var query = EntityQueryEnumerator<MapComponent>();
        while (query.MoveNext(out var mapUid, out _))
        {
            var persistence = EnsureComp<SectorWeatherPersistenceComponent>(mapUid);
            persistence.ActiveWeather = new Dictionary<SpaceSector, string>(_activeWeather);
        }
    }

    private void SetSectorWeatherCvar(SpaceSector sector, string weatherId)
    {
        _cfg.SetCVar(GetSectorWeatherCvar(sector), weatherId);
    }

    private static CVarDef<string> GetSectorWeatherCvar(SpaceSector sector)
    {
        return sector switch
        {
            SpaceSector.Center => CCVars.SectorWeatherCenter,
            SpaceSector.North => CCVars.SectorWeatherNorth,
            SpaceSector.NorthEast => CCVars.SectorWeatherNorthEast,
            SpaceSector.East => CCVars.SectorWeatherEast,
            SpaceSector.SouthEast => CCVars.SectorWeatherSouthEast,
            SpaceSector.South => CCVars.SectorWeatherSouth,
            SpaceSector.SouthWest => CCVars.SectorWeatherSouthWest,
            SpaceSector.West => CCVars.SectorWeatherWest,
            SpaceSector.NorthWest => CCVars.SectorWeatherNorthWest,
            _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, null),
        };
    }
}
