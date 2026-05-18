using Content.Server.Atmos.EntitySystems;
using Content.Server.MiningFluid.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.MiningFluid.Visuals;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.MiningFluid;

public sealed class TrappedFluidExtractionSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;


    private EntityQuery<TrappedFluidComponent> _trappedFluidQuery;

    public override void Initialize()
    {
        base.Initialize();

        _trappedFluidQuery = GetEntityQuery<TrappedFluidComponent>();

        SubscribeLocalEvent<TrappedFluidExtractorComponent, AtmosDeviceUpdateEvent>(OnExtractorUpdated);
        SubscribeLocalEvent<TrappedFluidExtractorComponent, AtmosDeviceEnabledEvent>(OnExtractorEnterAtmosphere);
        SubscribeLocalEvent<TrappedFluidExtractorComponent, AtmosDeviceDisabledEvent>(OnExtractorLeaveAtmosphere);
        SubscribeLocalEvent<TrappedFluidExtractorComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<TrappedFluidExtractorComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
        SubscribeLocalEvent<TrappedFluidExtractorComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<TrappedFluidExtractorComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);

    }

    private void OnAnchorStateChanged(EntityUid uid, TrappedFluidExtractorComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        SwitchOff(uid, component);
    }

    private void ReceivedChanged(
            EntityUid uid,
            TrappedFluidExtractorComponent component,
            ref PowerConsumerReceivedChanged args)
    {
        if (!component.IsOn)
        {
            return;
        }

        if (args.ReceivedPower < args.DrawRate)
        {
            PowerOff(uid, component);
        }
        else
        {
            PowerOn(uid, component);
        }
    }

    private void OnActivate(EntityUid uid, TrappedFluidExtractorComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (TryComp(uid, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
        {
            if (!component.IsOn)
            {
                SwitchOn(uid, component);
                _popup.PopupEntity(Loc.GetString("comp-emitter-turned-on",
                    ("target", uid)), uid, args.User);
            }
            else
            {
                SwitchOff(uid, component);
                _popup.PopupEntity(Loc.GetString("comp-emitter-turned-off",
                    ("target", uid)), uid, args.User);
            }

            _adminLogger.Add(LogType.FieldGeneration,
                component.IsOn ? LogImpact.Medium : LogImpact.High,
                $"{ToPrettyString(args.User):player} toggled {ToPrettyString(uid):emitter}");
            args.Handled = true;
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("comp-emitter-not-anchored",
                ("target", uid)), uid, args.User);
        }
    }

    public void SwitchOff(EntityUid uid, TrappedFluidExtractorComponent component)
    {
        component.IsOn = false;
        if (TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
            powerConsumer.DrawRate = 1; // this needs to be not 0 so that the visuals still work.
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcReceiver))
            apcReceiver.Load = 1;
        PowerOff(uid, component);
        UpdateState(uid, component);
    }

    public void SwitchOn(EntityUid uid, TrappedFluidExtractorComponent component)
    {
        component.IsOn = true;
        if (TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
            powerConsumer.DrawRate = component.PowerUseActive;
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcReceiver))
        {
            apcReceiver.Load = component.PowerUseActive;
            if (apcReceiver.Powered)
                PowerOn(uid, component);
        }
        // Do not directly PowerOn().
        // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.
        UpdateState(uid, component);
    }

    public void PowerOn(EntityUid uid, TrappedFluidExtractorComponent component)
    {
        if (component.IsPowered)
        {
            return;
        }

        component.IsPowered = true;

        UpdateState(uid, component);
    }

    public void PowerOff(EntityUid uid, TrappedFluidExtractorComponent component)
    {
        if (!component.IsPowered)
        {
            return;
        }

        component.IsPowered = false;

        UpdateState(uid, component);
    }

    private void OnExtractorUpdated(EntityUid uid, TrappedFluidExtractorComponent extractor, ref AtmosDeviceUpdateEvent args)
    {
        var timeDelta = args.dt;

        if (!extractor.IsPowered)
            return;

        if (!_nodeContainer.TryGetNode(uid, extractor.OutletName, out PipeNode? outlet))
            return;

        if (args.Grid is not { } grid)
            return;

        if (!_trappedFluidQuery.TryComp(grid, out var trappedFluid))
            return;
        var source = trappedFluid.Air;

        Extract(timeDelta, extractor, source, outlet);
    }

    private void OnExtractorLeaveAtmosphere(EntityUid uid, TrappedFluidExtractorComponent component,
        AtmosDeviceDisabledEvent args) => UpdateState(uid, component);

    private void OnExtractorEnterAtmosphere(EntityUid uid, TrappedFluidExtractorComponent component,
        AtmosDeviceEnabledEvent args) => UpdateState(uid, component);

    private void Extract(float timeDelta, TrappedFluidExtractorComponent extractor, GasMixture? source, PipeNode outlet)
    {
        Extract(timeDelta, extractor.TransferRate * _atmosphereSystem.PumpSpeedup(), extractor.MaxPressure, source, outlet.Air);
    }

    /// <summary>
    /// True if we were able to extract, false if we were not.
    /// </summary>
    public bool Extract(float timeDelta, float transferRate, float maxPressure, GasMixture? source, GasMixture destination)
    {
        // Cannot extract if source is null or air-blocked.
        if (source == null
            || destination.Pressure >= maxPressure) // Cannot extract if pressure too high.
        {
            return false;
        }

        // Take a gas sample.
        var ratio = MathF.Min(1f, timeDelta * transferRate / source.Volume);
        var removed = source.RemoveRatio(ratio);

        // Nothing left to remove from the tile.
        if (MathHelper.CloseToPercent(removed.TotalMoles, 0f))
            return false;

        // Remix the gases.
        _atmosphereSystem.Merge(destination, removed);
        return true;
    }

    private void OnPowerChanged(EntityUid uid, TrappedFluidExtractorComponent component, ref PowerChangedEvent args)
    {
        UpdateState(uid, component);
    }

    /// <summary>
    ///     Updates an extractors's appearance and ambience state.
    /// </summary>
    private void UpdateState(EntityUid uid, TrappedFluidExtractorComponent extractor,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        if (!extractor.IsPowered)
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            _appearance.SetData(uid, TrappedFluidExtractorVisuals.State, TrappedFluidExtractorState.Off, appearance);
        }
        else
        {
            _ambientSoundSystem.SetAmbience(uid, true);
            _appearance.SetData(uid, TrappedFluidExtractorVisuals.State, TrappedFluidExtractorState.On, appearance);
        }
    }

}
