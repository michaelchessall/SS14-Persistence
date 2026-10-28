using Content.Server.Power.EntitySystems;
using Content.Shared.Research.Components;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeClient()
    {
        SubscribeLocalEvent<MapInitEvent>(OnClientMapInit);
    }

    private void OnClientMapInit(ref MapInitEvent args)
    {
        var query = EntityQueryEnumerator<ResearchClientComponent>();
        while (query.MoveNext(out var uid, out var clientComp))
        {
            TryGetClientServer(uid, out _);
        }
    }
}
