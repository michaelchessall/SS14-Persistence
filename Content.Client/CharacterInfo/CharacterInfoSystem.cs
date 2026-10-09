using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.CharacterInfo;

public sealed partial class CharacterInfoSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;

    public event Action<CharacterData>? OnCharacterUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CharacterInfoEvent>(OnCharacterInfoEvent);
    }

    public void RequestCharacterInfo()
    {
        var entity = _players.LocalEntity;
        if (entity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestCharacterInfoEvent(GetNetEntity(entity.Value)));
    }

    public void UpdateDetailExaminable(string content)
    {
        RaiseNetworkEvent(new UpdateDetailExaminableEvent(content));
    }

    private void OnCharacterInfoEvent(CharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        var entity = GetEntity(msg.NetEntity);
        var data = new CharacterData
        {
            Entity = entity,
            Job = msg.Job ?? "Passanger",
            Faction = msg.Faction,
            BankBal = msg.BankBal,
            Objectives = msg.Objectives,
            Briefing = msg.Briefing,
            EntityName = Name(entity),
            Traits = msg.Traits
        };

        OnCharacterUpdate?.Invoke(data);
    }

    public List<Control> GetCharacterInfoControls(EntityUid uid)
    {
        var ev = new GetCharacterInfoControlsEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);
        return ev.Controls;
    }

    public readonly record struct CharacterData(
        EntityUid Entity,
        string Job,
        string? Faction,
        string BankBal,
        Dictionary<string, List<ObjectiveInfo>> Objectives,
        string? Briefing,
        string? DetailExaminable,
        string EntityName,
        List<ProtoId<TraitPrototype>> Traits
    )
    {
        public static CharacterData JohnDoe => new CharacterData(
            Entity: default,
            Objectives: new Dictionary<string, List<Shared.Objectives.ObjectiveInfo>>(),
            Briefing: null,
            Job: "Captain",
            EntityName: "John Doe",
            Faction: null,
            BankBal: "$0",
            DetailExaminable: null,
            Traits: new()
        );
    };

    /// <summary>
    /// Event raised to get additional controls to display in the character info menu.
    /// </summary>
    [ByRefEvent]
    public readonly record struct GetCharacterInfoControlsEvent(EntityUid Entity)
    {
        public readonly List<Control> Controls = new();

        public readonly EntityUid Entity = Entity;
    }
}
