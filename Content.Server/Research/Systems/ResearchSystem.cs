using Content.Server.Administration.Logs;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.Research;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Research.Systems
{
    [UsedImplicitly]
    public sealed partial class ResearchSystem : SharedResearchSystem
    {
        [Dependency] private IAdminLogManager _adminLog = default!;
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private AccessReaderSystem _accessReader = default!;
        [Dependency] private UserInterfaceSystem _uiSystem = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private RadioSystem _radio = default!;
        [Dependency] private StationSystem _station = default!;
        [Dependency] private PersistentIdentifierSystem _pid = default!;

        public override void Initialize()
        {
            base.Initialize();
            InitializeConsole();
            InitializeSource();

            SubscribeLocalEvent<TechnologyDatabaseComponent, ResearchRegistrationChangedEvent>(OnDatabaseRegistrationChanged);
        }
        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<ResearchServerComponent>();
            while (query.MoveNext(out var uid, out var server))
            {
                if (server.NextUpdateTime > _timing.CurTime)
                    continue;
                server.NextUpdateTime = _timing.CurTime + server.ResearchConsoleUpdateTime;

                UpdateServer(uid, (int)server.ResearchConsoleUpdateTime.TotalSeconds, server);
            }
        }
    }
}
