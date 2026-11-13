using Content.Server._Persistence14.GameTicking;
using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.PersistentIdentifier.Reference;
using Content.Shared.StationTeleporter;
using Content.Shared.StationTeleporter.Components;
using Robust.Shared.Timing;

namespace Content.Server.StationTeleporter.Systems;

/// <inheritdoc/>
public sealed partial class StationTeleporterSystem : SharedStationTeleporterSystem
{
    [Dependency] private IGameTiming _time = default!;

    private TimeSpan _nextUpdateTime = TimeSpan.Zero;
    private TimeSpan _updateInterval = TimeSpan.FromSeconds(1);

    private void AutoLink(Entity<StationTeleporterConsoleComponent> console)
    {
        if (console.Comp.AutoLinkKey is null || console.Comp.AutoLinkChipsProto is null)
            return;

        var query = EntityQueryEnumerator<StationTeleporterComponent>();
        while (query.MoveNext(out var teleporterUid, out var teleporter))
        {
            if (teleporter.AutoLinkKey is null || console.Comp.AutoLinkKey != teleporter.AutoLinkKey)
                continue;

            //Spawn chip inside this console
            var chipEnt = SpawnInContainerOrDrop(console.Comp.AutoLinkChipsProto, console, console.Comp.ChipStorageName);
            if (TryComp<TeleporterChipComponent>(chipEnt, out var chipComp))
            {
                ConnectChipToTeleporter((chipEnt, chipComp), (teleporterUid, teleporter));
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnPostInit(ref PostInitEvent args)
    {
        var portalQuery = EntityQueryEnumerator<StationTeleporterComponent>();
        while (portalQuery.MoveNext(out var uid, out var teleporter))
        {
            if (!TryRestoreLink((uid, teleporter)))
                teleporter.LastLink = PersistentIdentifierSystem.EmptyId;

            RefreshVisuals((uid, teleporter));
        }

        var consoleQuery = EntityQueryEnumerator<StationTeleporterConsoleComponent>();
        while (consoleQuery.MoveNext(out var uid, out var console))
            AutoLink((uid, console));
    }

    public override void Update(float frameTime)
    {
        if (_time.CurTime < _nextUpdateTime)
            return;
        _nextUpdateTime = _time.CurTime + _updateInterval;

        RefreshAllConsoles();
    }
}
