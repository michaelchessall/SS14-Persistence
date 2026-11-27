using System.Collections.Generic;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Salvage.Magnet;
using Content.Shared.Salvage.Magnet;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests;

public sealed class SalvageMagnetTetherTest : GameTest
{
    [EnsureCVar(Side.Server, typeof(Robust.Shared.CVars), nameof(Robust.Shared.CVars.NetPVS), true)]
    [TestCase("magnet")]
    [TestCase("target")]
    [TestCase("expiry")]
    public async Task TetherTracksUnloadedEndpointsAndCleansUp(string endReason)
    {
        var map = await Pair.CreateTestMap();
        EntityUid magnet = default;
        EntityUid target = default;
        EntityUid visual = default;
        SalvageMagnetDataComponent data = default!;
        NetEntity visualNet = default;
        NetEntity magnetNet = default;
        var start = new MapCoordinates(new Vector2(1000, 1000), map.MapId);
        var end = new MapCoordinates(new Vector2(1100, 1000), map.MapId);

        await Server.WaitPost(() =>
        {
            magnet = SEntMan.SpawnEntity(null, start);
            target = SEntMan.SpawnEntity(null, end);
            data = SEntMan.AddComponent<SalvageMagnetDataComponent>(magnet);
            data.EndTime = TimeSpan.MaxValue;
            data.NextOffer = TimeSpan.MaxValue;
            data.ActiveEntities = new List<EntityUid> { target };

            visual = SEntMan.SpawnEntity(null, MapCoordinates.Nullspace);
            var source = SEntMan.AddComponent<SalvageMagnetTetherSourceComponent>(visual);
            source.Magnet = magnet;
            source.Target = target;
            source.Data = magnet;
            var tether = SEntMan.AddComponent<SalvageMagnetTetherComponent>(visual);
            tether.Sprite = new SpriteSpecifier.Rsi(
                new ResPath("Structures/Power/Generation/Singularity/containment_field.rsi"), "field");
            SEntMan.System<PvsOverrideSystem>().AddGlobalOverride(visual);
            visualNet = SEntMan.GetNetEntity(visual);
            magnetNet = SEntMan.GetNetEntity(magnet);
        });

        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var tether = CEntMan.GetComponent<SalvageMagnetTetherComponent>(CEntMan.GetEntity(visualNet));
            Assert.That(tether.Start, Is.EqualTo(start));
            Assert.That(tether.End, Is.EqualTo(end));
            Assert.That(CEntMan.TryGetEntity(magnetNet, out _), Is.False,
                "The beam must not require the magnet to be loaded on the client.");
        });

        end = new MapCoordinates(new Vector2(1120, 1020), map.MapId);
        await Server.WaitPost(() => SEntMan.System<SharedTransformSystem>().SetMapCoordinates(target, end));
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var tether = CEntMan.GetComponent<SalvageMagnetTetherComponent>(CEntMan.GetEntity(visualNet));
            Assert.That(tether.End, Is.EqualTo(end));
        });

        await Server.WaitPost(() =>
        {
            switch (endReason)
            {
                case "magnet": SEntMan.DeleteEntity(magnet); break;
                case "target": SEntMan.DeleteEntity(target); break;
                case "expiry": data.EndTime = TimeSpan.Zero; break;
            }
        });
        await RunUntilSynced();
        await Server.WaitAssertion(() => Assert.That(SEntMan.Deleted(visual), Is.True));
        await Client.WaitAssertion(() => Assert.That(CEntMan.TryGetEntity(visualNet, out _), Is.False));
    }
}
