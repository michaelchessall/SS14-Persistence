using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.CrewRecords.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Persistence14.Resonance.Prototypes;
using Content.Shared._Persistence14.Rumors.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Parallax;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Exceptions;

namespace Content.Server._Persistence14.Resonance.Systems;

public sealed partial class ResonanceSystem : EntitySystem
{
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private CrewMetaRecordsSystem _crewMeta = default!;
    [Dependency] private EntityQuery<StationDataComponent> _stationDataQuery = default!;
    [Dependency] private EntityQuery<MapBoundsComponent> _mapBoundsQuery = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private CargoSystem _cargo = default!;
    public void OnResonancePurchase(Entity<StationModificationConsoleComponent> ent, StationModificationResonancePurchase args)
    {
        var faction = _station.GetOwningStation(ent);

        if (faction == null || _crewMeta.MetaRecords == null) return;
        if (!_stationDataQuery.TryComp(faction, out var stationData)) return;
        var proto = _protoMan.Index(args.Prototype);

        if (proto.Unique && _crewMeta.MetaRecords.ResonancePurchases.Contains(args.Prototype)) return;
        if (proto.Requires != null && !_crewMeta.MetaRecords.ResonancePurchases.Contains(proto.Requires.Value)) return;
        if (proto.Shared)
        {
            if (stationData.StoredResonance < 100) return;
            stationData.StoredResonance -= 100;
            if(_crewMeta.MetaRecords.PartialPurchases.ContainsKey(args.Prototype))
            {
                _crewMeta.MetaRecords.PartialPurchases[args.Prototype] += 100;
                if (_crewMeta.MetaRecords.PartialPurchases[args.Prototype] >= proto.Price)
                {
                    FinishPurchase(proto, faction);
                    _crewMeta.MetaRecords.PartialPurchases.Remove(args.Prototype);
                }
            }
            else
            {
                _crewMeta.MetaRecords.PartialPurchases[args.Prototype] = 100;
            }
        }
        else
        {
            if (stationData.StoredResonance < proto.Price) return;
            if(FinishPurchase(proto, faction))
            {
                stationData.StoredResonance -= proto.Price;
            }
        }
    }

    private bool FinishPurchase(ResonanceProductPrototype proto, EntityUid? faction)
    {
        if (_crewMeta.MetaRecords == null) return false;
        if (proto.ProductType == ResonanceProductType.MainWorldBoundary)
        {
            if (!_mapBoundsQuery.TryComp(_crewMeta.MetaRecords.Owner, out var mapBounds)) return false;
            mapBounds.Radius += proto.RadiusExpansion;
            _chat.DispatchGlobalAnnouncement($"A sector expansion has been triggered; The Sector Boundary has grown by {proto.RadiusExpansion}m!", "Sector News");
        }
        if(proto.ProductType == ResonanceProductType.MainWorldAwakening)
        {
            _crewMeta.MetaRecords.SectorAwoke = true;
            _chat.DispatchGlobalAnnouncement($"Surges of electricity crackle through space! The Sector has AWOKE; Tier 3 Insights are now available!", "Sector News");
            var parallax = EnsureComp<ParallaxComponent>(_crewMeta.MetaRecords.Owner);
            parallax.Parallax = "Wizard";
        }
        if(proto.ProductType == ResonanceProductType.Product)
        {
            if (faction == null) return false;
            var tradeStation = _station.GetStationTradeStation(faction.Value);
            if (tradeStation == null) return false;

            if (_cargo.TryFulfillOrderResonance(proto, "Paper", tradeStation.Value) == null)
            {
                return false;
            }
            else
            {
                _chat.DispatchGlobalAnnouncement($"A truly unique artifact has appeared in the Sector. {proto.Name} has been delivered to {Name(tradeStation.Value)}", "Sector News");
            }
        }
        if (proto.Unique)
        {
            _crewMeta.MetaRecords.ResonancePurchases.Add(proto.ID);
        }
        return true;

    }
}
