using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.CrewRecords.Systems;
using Content.Server.EUI;
using Content.Shared.CrewAssignments.Systems;
using Content.Shared.Eui;
using Content.Shared.Follower;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.CrewAssignments.AdminUI;

public sealed class CodexEui : BaseEui
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private readonly CrewMetaRecordsSystem _crewMeta;
    private readonly ChatSystem _chat;
    [Dependency] private readonly IChatManager _chatInterface = default!;
    public CodexEui()
    {
        IoCManager.InjectDependencies(this);
        _crewMeta = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<CrewMetaRecordsSystem>();
        _chat = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override CodexEuiState GetNewState()
    {
        if (_crewMeta.MetaRecords == null) return new CodexEuiState(new List<CodexEntry>(), "");
        return new CodexEuiState(_crewMeta.MetaRecords.CodexEntries, _crewMeta.MetaRecords.SectorStatus);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (_crewMeta.MetaRecords == null) return;

        switch (msg)
        {
            case CodexEuiMsg.Reveal createData:
                {
                    foreach (var entry in _crewMeta.MetaRecords.CodexEntries)
                    {
                        if (entry.ID == createData.ID)
                        {
                            entry.Visible = !entry.Visible;
                            if (entry.Visible)
                            {
                                _chat.DispatchGlobalAnnouncement($"A codex entry has been permanently unlocked for everyone! {entry.Title}", "Threshold Codex");
                            }
                        }
                    }
                    break;
                }
            case CodexEuiMsg.Save saveData:
                {
                    if(saveData.ID == -1)
                    {
                        _crewMeta.MetaRecords.SectorStatus = saveData.Description;
                        _chat.DispatchGlobalAnnouncement($"The Sector Status has changed!", "Sector Status");
                        break;
                    }
                    foreach (var entry in _crewMeta.MetaRecords.CodexEntries)
                    {
                        if (entry.ID == saveData.ID)
                        {
                            entry.Title = saveData.Title;
                            entry.Description = saveData.Description;
                        }
                    }
                    break;
                }
            case CodexEuiMsg.SaveWhitelist createData:
                {
                    foreach (var entry in _crewMeta.MetaRecords.CodexEntries)
                    {
                        if (entry.ID == createData.ID)
                        {

                            entry.Whitelist.Add(createData.Name);
                            var actorQuery = _entityManager.EntityQueryEnumerator<ActorComponent>();
                            while (actorQuery.MoveNext(out _, out var actorComp))
                            {
                                MetaDataComponent? metaData = null;
                                if (!_entityManager.MetaQuery.Resolve(actorComp.Owner, ref metaData, false))
                                    continue;
                                var name = metaData.EntityName;
                                if (name == createData.Name && actorComp.PlayerSession != null)
                                {
                                    _chatInterface.DispatchServerMessage(actorComp.PlayerSession, $"A codex entry has been permanently unlocked for you! {entry.Title}");
                                }
                            }
                        }
                    }
                    break;
                }
            case CodexEuiMsg.DeleteWhitelist createData:
                {

                    foreach (var entry in _crewMeta.MetaRecords.CodexEntries)
                    {
                        if (entry.ID == createData.ID)
                        {
                            entry.Whitelist.Remove(createData.Name);
                        }
                    }
                    break;
                }
            case CodexEuiMsg.CreateNew createData:
                {
                    CodexEntry entry = new(_crewMeta.MetaRecords.NextCodexID);
                    _crewMeta.MetaRecords.NextCodexID++;
                    _crewMeta.MetaRecords.CodexEntries.Add(entry);
                    break;
                }


            case CodexEuiMsg.Delete deleteData:
                {
                    foreach (var entry in _crewMeta.MetaRecords.CodexEntries.ShallowClone())
                    {
                        if (entry.ID == deleteData.ID)
                        {
                            _crewMeta.MetaRecords.CodexEntries.Remove(entry);
                        }
                    }
                    break;
                }

        }
        StateDirty();
    }
}

