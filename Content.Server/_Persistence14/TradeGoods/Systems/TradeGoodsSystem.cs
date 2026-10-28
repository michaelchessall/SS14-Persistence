using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Salvage.Magnet;
using Content.Server.Station.Systems;
using Content.Shared._Persistence14.Rumors.Components;
using Content.Shared._Persistence14.TradeGoods.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Power;
using Content.Shared.Prayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server._Persistence14.TradeGoods.Systems;


public sealed partial class TradeGoodsSystem : EntitySystem
{
    [Dependency] private EntityQuery<TradeStationComponent> _tradeStationQuery = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private ChatSystem _chat = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TradeGoodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TradeGoodComponent, GridUidChangedEvent>(OnTradeGoodGridChange);
        SubscribeLocalEvent<TradeGoodComponent, PriceCalculationEvent>(OnGetPrice);
        SubscribeLocalEvent<TradeGoodComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<TradeGoodComponent> ent, ref ComponentShutdown args)
    {
        var grid = Transform(ent).GridUid;
        if (grid != null)
        {
            if (_tradeStationQuery.TryComp(grid.Value, out var tradeStation))
            {
                RecountTradeGoods(grid.Value, tradeStation);
            }
        }
    }
    private void OnGetPrice(Entity<TradeGoodComponent> ent, ref PriceCalculationEvent args)
    {
        args.Price = ent.Comp.PriceOverride;
        args.Handled = true;
    }

    private void OnTradeGoodGridChange(Entity<TradeGoodComponent> ent, ref GridUidChangedEvent args)
    {
        if (args.NewGrid != null)
        {
            if (_tradeStationQuery.TryComp(args.NewGrid.Value, out var tradeStation))
            {
                RecountTradeGoods(args.NewGrid.Value, tradeStation);
            }
        }
        if (args.OldGrid != null)
        {
            if (_tradeStationQuery.TryComp(args.OldGrid.Value, out var tradeStation))
            {
                RecountTradeGoods(args.OldGrid.Value, tradeStation);
            }
        }
    }

    private void RecountTradeGoods(EntityUid ent, TradeStationComponent comp, bool initCount = false)
    {
        var query = EntityQueryEnumerator<TradeGoodComponent>();
        comp.ExperiencePoints = 0;
        while (query.MoveNext(out var uid, out _))
        {
            if (Transform(uid).GridUid == ent)
            {
                comp.ExperiencePoints++;
            }
        }
        if (comp.ExperiencePoints < 5)
        {
            var station = _station.GetOwningStation(comp.Owner);
            if (station != null && !initCount)
            {
                _station.RemoveGridFromStation(station.Value, comp.Owner);
                _station.AnnounceTradeStationRelease(comp);
            }
        }
    }

    private void OnComponentInit(Entity<TradeGoodComponent> ent, ref ComponentInit args)
    {
        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
        var grid = Transform(ent).GridUid;
        if (grid != null)
        {
            if (_tradeStationQuery.TryComp(grid.Value, out var tradeStation))
            {
                RecountTradeGoods(grid.Value, tradeStation, true);
            }
        }

    }
}
