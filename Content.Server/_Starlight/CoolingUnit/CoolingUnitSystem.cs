using Content.Server.Body.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Starlight.CoolingUnit;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.CoolingUnit;

public sealed partial class CoolingUnitSystem : SharedCoolingUnitSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TemperatureSystem _tempSys = default!;
    [Dependency] private PersistentIdentifierSystem _pid = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private TimeSpan _lastUpdate = TimeSpan.Zero;
    private readonly TimeSpan _updateCooldown = TimeSpan.FromSeconds(1f);

    public override void Initialize()
    {
        base.Initialize();

        _lastUpdate = _timing.CurTime;
        _nextUpdate = _timing.CurTime + _updateCooldown;

        SubscribeLocalEvent<CoolingUnitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<CoolingUnitComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<CoolingUnitComponent, ComponentStartup>(OnComponentStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime > _nextUpdate)
        {
            _nextUpdate = _timing.CurTime + _updateCooldown;
            var timeSince = _timing.CurTime - _lastUpdate;
            _lastUpdate = _timing.CurTime;

            var query = EntityQueryEnumerator<CoolingUnitComponent>();

            while (query.MoveNext(out var uid, out var coolingUnit))
            {
                if (!TryComp<ItemToggleComponent>(uid, out var toggle) ||
                    !toggle.Activated ||
                    !_pid.TryResolveId(coolingUnit.CoolingTarget, out var target) ||
                    !TryComp<TemperatureComponent>(target, out var temperatureComponent) ||
                    !TryComp<ThermalRegulatorComponent>(target, out var thermalRegulatorComponent) ||
                    temperatureComponent.CurrentTemperature <= thermalRegulatorComponent.NormalBodyTemperature)
                    continue;


                var coolingAmount = Math.Min(coolingUnit.MaxCooling * (float)timeSince.TotalSeconds, temperatureComponent.CurrentTemperature - thermalRegulatorComponent.NormalBodyTemperature);
                _tempSys.ForceChangeTemperature(target, temperatureComponent.CurrentTemperature - coolingAmount, temperatureComponent);
            }
        }
    }

    private void OnComponentStartup(EntityUid uid, CoolingUnitComponent component, ref ComponentStartup args)
    {
        var parent = Transform(uid).ParentUid;

        if (!Exists(uid))
            return;

        if (_hands.IsHolding(parent, uid))
        {
            component.CoolingTarget = _pid.EnsureId(parent);
            Dirty(uid, component);
            return;
        }

        if (_inventory.TryGetContainingSlot(uid, out _))
        {
            component.CoolingTarget = _pid.EnsureId(parent);
            Dirty(uid, component);
            return;
        }
    }

    private void OnEquipped(EntityUid uid, CoolingUnitComponent component, ref GotEquippedEvent args)
    {
        component.CoolingTarget = _pid.EnsureId(args.Equipee);
        Dirty(uid, component);
    }

    private void OnUnequipped(EntityUid uid, CoolingUnitComponent component, ref GotUnequippedEvent args)
    {
        component.CoolingTarget = PersistentIdentifierSystem.EmptyId;
        Dirty(uid, component);
    }
}
