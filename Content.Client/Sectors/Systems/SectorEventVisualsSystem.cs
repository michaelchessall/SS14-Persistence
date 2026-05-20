using Content.Client.Sectors.Overlays;
using Content.Shared.Sectors;
using Content.Shared.Sectors.Events;
using Content.Shared.Sectors.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Sectors.Systems;

public sealed class SectorEventVisualsSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedSectorSystem _sectors = default!;

    private readonly Dictionary<SpaceSector, string> _activeWeather = new();
    private SectorEventOverlay _overlay = default!;

    private Color _targetTintColor = Color.Transparent;
    private float _targetAlpha;
    private float _currentAlpha;
    private float _targetNoiseStrength;
    private float _currentNoiseStrength;

    private const float FadeSpeed = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new SectorEventOverlay();
        _overlays.AddOverlay(_overlay);

        SubscribeNetworkEvent<SectorWeatherStateUpdateEvent>(OnWeatherStateUpdated);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlays.RemoveOverlay(_overlay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_players.LocalEntity is not { } player)
        {
            _targetAlpha = 0f;
            _targetNoiseStrength = 0f;
        }
        else
        {
            var sector = _sectors.GetSector(player);
            if (!_activeWeather.TryGetValue(sector, out var weatherId) ||
                !_prototypes.TryIndex<SectorWeatherPrototype>(weatherId, out var weather))
            {
                _targetAlpha = 0f;
                _targetNoiseStrength = 0f;
            }
            else
            {
                var color = weather.ScreenTintColor;
                var strength = Math.Clamp(weather.ScreenTintStrength, 0f, 1f);
                _targetTintColor = color;
                _targetAlpha = color.A * strength;
                _targetNoiseStrength = weather.ScreenTintNoiseStrength;
            }
        }

        var t = Math.Min(1f, frameTime * FadeSpeed);
        _currentAlpha += (_targetAlpha - _currentAlpha) * t;
        _currentNoiseStrength += (_targetNoiseStrength - _currentNoiseStrength) * t;

        _overlay.TintColor = _targetTintColor.WithAlpha(_currentAlpha);
        _overlay.TintNoiseStrength = _currentNoiseStrength;
    }

    private void OnWeatherStateUpdated(SectorWeatherStateUpdateEvent ev)
    {
        _activeWeather.Clear();
        foreach (var (sector, weatherId) in ev.ActiveWeather)
        {
            _activeWeather[sector] = weatherId;
        }
    }
}
