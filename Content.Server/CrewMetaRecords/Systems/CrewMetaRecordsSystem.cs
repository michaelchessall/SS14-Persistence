using Content.Server.Access.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.CrewMetaRecords;
using Robust.Shared.Player;

namespace Content.Server.CrewRecords.Systems;

public sealed partial class CrewMetaRecordsSystem : SharedCrewMetaRecordsSystem
{
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IdCardSystem _idCard = default!;

    public bool CharacterNameExists(string name)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound) return true;
        return MetaRecords != null && MetaRecords.CrewMetaRecords.ContainsKey(name);
    }

    public void JoinFirstTime(ICommonSession session)
    {
        _gameTicker!.MakeJoinGamePersistent(session);
    }

    public void DevalidateID(string name)
    {
        if (MetaRecords != null && MetaRecords.CrewMetaRecords.TryGetValue(name, out var record))
        {
            record.LatestIDTime = DateTime.Now;
            _idCard.ExpireAllIds(name);
        }

    }

}
