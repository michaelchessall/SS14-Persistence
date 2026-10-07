using Content.Server.Salvage.Magnet;
using Content.Shared._Persistence14.Rumors.Components;
using Content.Shared._Persistence14.TradeGoods.Components;
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
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TradeGoodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TradeGoodComponent, GridUidChangedEvent>(OnTradeGoodGridChange);
    }

    private void OnTradeGoodGridChange(Entity<TradeGoodComponent> ent, ref GridUidChangedEvent args)
    {
        if(args.NewGrid != null)
        {
            if(_tradeStationQuery.TryComp(args.NewGrid.Value, out var tradeStation))
            {
                RecountTradeGoods(args.NewGrid.Value, tradeStation);
            }
        }
        if(args.OldGrid != null)
        {
            if (_tradeStationQuery.TryComp(args.OldGrid.Value, out var tradeStation))
            {
                RecountTradeGoods(args.OldGrid.Value, tradeStation);
            }
        }
    }

    private void RecountTradeGoods(EntityUid ent, TradeStationComponent comp)
    {
        var query = EntityQueryEnumerator<TradeGoodComponent>();
        comp.ExperiencePoints = 0;
        while (query.MoveNext(out var uid, out _))
        {
            if(Transform(uid).GridUid == ent)
            {
                comp.ExperiencePoints++;
            }
        }
    }

    private void OnComponentInit(Entity<TradeGoodComponent> ent, ref ComponentInit args)
    {
        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
    }
}
