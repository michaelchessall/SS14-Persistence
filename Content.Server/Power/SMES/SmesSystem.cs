using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rounding;
using Content.Shared.SMES;
using JetBrains.Annotations;
using Robust.Shared.Timing;
using Content.Server.DeviceLinking.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Power.SMES;

[UsedImplicitly]
internal sealed partial class SmesSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<SmesComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SmesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SmesComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
    }

    private void OnInit(EntityUid uid, SmesComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, comp.ChargeLevel0Port, comp.ChargeLevel1Port, comp.ChargeLevel2Port, comp.ChargeLevel3Port, comp.ChargeLevel4Port, comp.ChargeLevel5Port);
    }

    private void OnMapInit(EntityUid uid, SmesComponent component, MapInitEvent args)
    {
        UpdateSmesState(uid, component);
        _deviceLink.SendSignal(uid, GetPort(component, component.ChargeLevel), true);
    }

    private void OnBatteryChargeChanged(EntityUid uid, SmesComponent component, ref ChargeChangedEvent args)
    {
        UpdateSmesState(uid, component);
    }

    private void UpdateSmesState(EntityUid uid, SmesComponent smes)
    {
        var newLevel = CalcChargeLevel(uid);
        if (newLevel != smes.LastChargeLevel && smes.LastChargeLevelTime + smes.VisualsChangeDelay < _gameTiming.CurTime)
        {
            _deviceLink.SendSignal(uid, GetPort(smes, smes.LastChargeLevel), false);
            smes.LastChargeLevel = newLevel;
            smes.LastChargeLevelTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeLevel, newLevel);
            _deviceLink.SendSignal(uid, GetPort(smes, newLevel), true);
        }

        var newChargeState = CalcChargeState(uid);
        if (newChargeState != smes.LastChargeState && smes.LastChargeStateTime + smes.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeState = newChargeState;
            smes.LastChargeStateTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeState, newChargeState);
        }
    }

    private int CalcChargeLevel(EntityUid uid, BatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref battery, false))
            return 0;

        var currentCharge = _battery.GetCharge((uid, battery));
        return ContentHelpers.RoundToLevels(currentCharge, battery.MaxCharge, 6);
    }

    private ChargeState CalcChargeState(EntityUid uid, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(uid, ref netBattery, false))
            return ChargeState.Still;

        return (netBattery.CurrentSupply - netBattery.CurrentReceiving) switch
        {
            > 0 => ChargeState.Discharging,
            < 0 => ChargeState.Charging,
            _ => ChargeState.Still
        };
    }

    private string GetPort(SmesComponent comp, int chargeLevel)
    {
        switch (chargeLevel)
        {
            case 0:
                return comp.ChargeLevel0Port;
            case 1:
                return comp.ChargeLevel1Port;
            case 2:
                return comp.ChargeLevel2Port;
            case 3:
                return comp.ChargeLevel3Port;
            case 4:
                return comp.ChargeLevel4Port;
            case 5:
                return comp.ChargeLevel5Port;
        }
        return "invalid chargelevel";
    }
}
