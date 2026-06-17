using Content.Client.Sectors.Overlays;
using Content.Client.Parallax;
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
    [Dependency] private readonly ParallaxSystem _parallaxSystem = default!;
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
    private SpaceSector? _lastSector;
    private string? _lastWeatherId;
    private string? _activeParallaxOverride;

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
        _parallaxSystem.SetLocalParallaxOverride(null);
        _overlays.RemoveOverlay(_overlay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_players.LocalEntity is not { } player)
        {
            _targetAlpha = 0f;
            _targetNoiseStrength = 0f;
            ApplyParallaxOverrideIfChanged(null, null);
        }
        else
        {
            var sector = _sectors.GetSector(player);
            if (!_activeWeather.TryGetValue(sector, out var weatherId) ||
                !_prototypes.TryIndex<SectorWeatherPrototype>(weatherId, out var weather))
            {
                _targetAlpha = 0f;
                _targetNoiseStrength = 0f;
                ApplyParallaxOverrideIfChanged(sector, null);
            }
            else
            {
                var color = weather.ScreenTintColor;
                var strength = Math.Clamp(weather.ScreenTintStrength, 0f, 1f);
                _targetTintColor = color;
                _targetAlpha = color.A * strength;
                _targetNoiseStrength = weather.ScreenTintNoiseStrength;
                ApplyParallaxOverrideIfChanged(sector, weatherId, weather.Parallax);
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

    private void ApplyParallaxOverrideIfChanged(SpaceSector? sector, string? weatherId, string? weatherParallax = null)
    {
        if (_lastSector == sector && _lastWeatherId == weatherId)
            return;

        _lastSector = sector;
        _lastWeatherId = weatherId;

        var nextParallaxOverride = string.IsNullOrWhiteSpace(weatherParallax) ? null : weatherParallax;
        if (_activeParallaxOverride == nextParallaxOverride)
            return;

        _activeParallaxOverride = nextParallaxOverride;
        _parallaxSystem.SetLocalParallaxOverride(_activeParallaxOverride);
    }
}
