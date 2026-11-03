using Content.Server.DeviceLinking.Components;
using Content.Server.Doors.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using JetBrains.Annotations;

namespace Content.Server.DeviceLinking.Systems
{
    [UsedImplicitly]
    public sealed partial class DoorSignalControlSystem : EntitySystem
    {
        [Dependency] private DoorSystem _doorSystem = default!;
        [Dependency] private DeviceLinkSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DoorSignalControlComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DoorSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<DoorSignalControlComponent, DoorStateChangedEvent>(OnStateChanged);
        }

        private void OnInit(EntityUid uid, DoorSignalControlComponent component, ComponentInit args)
        {

            _signalSystem.EnsureSinkPorts(uid, component.OpenPort, component.ClosePort, component.TogglePort);
            _signalSystem.EnsureSourcePorts(uid, component.OutOpen);
        }

        private void OnSignalReceived(EntityUid uid, DoorSignalControlComponent component, ref SignalReceivedEvent args)
        {
            if (!TryComp(uid, out DoorComponent? door))
                return;

            var state = SignalState.Momentary;
            args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

            // A special "fuzzy" helper state, which equates to either High or Momentary(pulse signal).
            // Used for signals that respond on prompt rather than sustained.
            bool fuzzyState = state == SignalState.High || state == SignalState.Momentary;
            switch (args.Port)
            {
                case var port when port == component.OpenPort && door.State == DoorState.Closed && fuzzyState:
                    _doorSystem.TryOpen(uid, door);
                    break;
                case var port when port == component.ClosePort && door.State == DoorState.Open && fuzzyState:
                    _doorSystem.TryClose(uid, door);
                    break;
                case var port when port == component.TogglePort && fuzzyState:
                    _doorSystem.TryToggleDoor(uid, door);
                    break;
                case var port when port == component.InBolt && TryComp<DoorBoltComponent>(uid, out var bolts):
                    switch (state)
                    {
                        case SignalState.Momentary:
                            _doorSystem.SetBoltsDown((uid, bolts), !bolts.BoltsDown);
                            break;
                        case SignalState.High:
                            _doorSystem.SetBoltsDown((uid, bolts), true);
                            break;
                        case SignalState.Low:
                            _doorSystem.SetBoltsDown((uid, bolts), false);
                            break;
                    }
                    break;
            }
        }

        private void OnStateChanged(EntityUid uid, DoorSignalControlComponent door, DoorStateChangedEvent args)
        {
            if (args.State == DoorState.Closed)
            {
                // only ever say the door is closed when it is completely airtight
                _signalSystem.SendSignal(uid, door.OutOpen, false);
            }
            else if (args.State == DoorState.Open
                  || args.State == DoorState.Opening
                  || args.State == DoorState.Closing
                  || args.State == DoorState.Emagging
                  || args.State == DoorState.boltingOpen)
            {
                // say the door is open whenever it would be letting air pass
                _signalSystem.SendSignal(uid, door.OutOpen, true);
            }
        }
    }
}
