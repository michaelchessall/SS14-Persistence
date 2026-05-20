using Content.Server.Administration.Logs;
using Content.Server.Sectors.Events;
using Content.Shared.Database;
using Content.Shared.Sectors.Events;
using Content.Shared.Sectors;
using Content.Shared.Sectors.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Log;

namespace Content.Server.Sectors.Systems;

/// <summary>
/// Tracks active sector weather events and broadcasts changes for UI systems.
/// </summary>
public sealed class SectorWeatherSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    private readonly Dictionary<SpaceSector, string> _activeWeather = new();

    public override void Initialize()
    {
        base.Initialize();
        _activeWeather.Clear();
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
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
        RaiseLocalEvent(new SectorWeatherChangedEvent(sector, weatherId));
        BroadcastWeatherState();
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"Sector weather event '{weatherId}' set for sector {sector}.");
        return true;
    }

    public bool ClearWeather(SpaceSector sector)
    {
        if (!_activeWeather.Remove(sector, out var clearedId))
            return false;

        RaiseLocalEvent(new SectorWeatherChangedEvent(sector, null));
        BroadcastWeatherState();
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"Sector weather event '{clearedId}' cleared from sector {sector}.");
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
}
