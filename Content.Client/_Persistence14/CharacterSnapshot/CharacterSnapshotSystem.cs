using System.Diagnostics.CodeAnalysis;
using Content.Client.Lobby;
using Content.Shared._Persistence14.CharacterSnapshot;
using Robust.Client.State;
using Robust.Client.UserInterface;

namespace Content.Client._Persistence14.CharacterSnapshot;

/// <summary>
/// Holds the "last seen" snapshots the server sent for this player's characters, for the lobby preview.
/// </summary>
public sealed partial class CharacterSnapshotSystem : EntitySystem
{
    [Dependency] private IStateManager _state = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private Dictionary<string, CharacterAppearanceSnapshot> _snapshots = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CharacterSnapshotsEvent>(OnSnapshots);
    }

    private void OnSnapshots(CharacterSnapshotsEvent ev)
    {
        _snapshots = ev.Snapshots;

        // The lobby usually builds its previews before this arrives, so rebuild them.
        if (_state.CurrentState is LobbyState)
            _ui.GetUIController<LobbyUIController>().ReloadCharacterSetup();
    }

    public bool TryGetSnapshot(string characterName, [NotNullWhen(true)] out CharacterAppearanceSnapshot? snapshot)
    {
        return _snapshots.TryGetValue(characterName, out snapshot);
    }
}
