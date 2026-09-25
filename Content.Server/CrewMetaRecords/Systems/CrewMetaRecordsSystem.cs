using Content.Server.Access.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.CrewMetaRecords;
using Content.Shared.Preferences;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.CrewRecords.Systems;

public sealed partial class CrewMetaRecordsSystem : SharedCrewMetaRecordsSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private HumanoidCharacterProfile _profile = new();

    public bool CharacterNameExists(string name)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound) return true;
        if (_config.GetCVar(CCVars.RestrictedNames))
            _profile.ApplyRestrictedNameRegex(ref name);
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
