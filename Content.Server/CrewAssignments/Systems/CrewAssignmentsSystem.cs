using Content.Server.Cargo.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewAssignments.Systems;
using Content.Shared.Paper;
using Content.Shared.Station;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;


namespace Content.Server.CrewAssignments.Systems;

public sealed partial class CrewAssignmentSystem : SharedCrewAssignmentSystem
{
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private StationSystem _station2 = default!;
    [Dependency] private CargoSystem _cargo = default!;



    public override void Initialize()
    {
        base.Initialize();
        InitializeConsole();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }




}
