using Content.Server.Chat.Systems;
using Content.Server.CrewRecords.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Coordinates;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using static Content.Shared.Access.Components.IdPrinterConsoleComponent;

namespace Content.Server.Access.Systems;

[UsedImplicitly]
public sealed class IdPrinterConsoleSystem : SharedIdPrinterConsoleSystem
{
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private IdCardSystem _idCard = default!;
    [Dependency] private CrewMetaRecordsSystem _crewMeta = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private HandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdPrinterConsoleComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<IdPrinterConsoleComponent, PrintID>(Print);
    }

    private void Print(EntityUid uid, IdPrinterConsoleComponent component, PrintID args)
    {
        if (args.Actor is not { Valid: true } player)
            return;
        var name = Name(player);
        if (_crewMeta.MetaRecords != null && _crewMeta.MetaRecords.CrewMetaRecords.ContainsKey(name))
        {
            _crewMeta.DevalidateID(name);
        }
        var iD = _entityManager.SpawnAtPosition("PassengerIDCard", player.ToCoordinates());

        if (!_hands.TryPickupAnyHand(player, iD))
            _transform.SetLocalRotation(iD, Angle.Zero); // Orient these to grid north instead of map north
        _idCard.BuildID(iD, name);

    }
    private void UpdateUserInterface(EntityUid uid, IdPrinterConsoleComponent component, EntityEventArgs args)
    {
        IdPrinterConsoleBoundUserInterfaceState newState = new();
        _userInterface.SetUiState(uid, IdPrinterConsoleUiKey.Key, newState);
    }

}
