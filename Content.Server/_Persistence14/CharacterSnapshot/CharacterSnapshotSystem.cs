using System.IO;
using System.Linq;
using Content.Server.GameTicking;
using Content.Shared._Persistence14.CharacterSnapshot;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Server._Persistence14.CharacterSnapshot;

/// <summary>
/// Writes a small cosmetic snapshot next to each character save, and sends a player's snapshots to them in the lobby
/// so the character preview shows them as they were last seen.
/// </summary>
public sealed partial class CharacterSnapshotSystem : EntitySystem
{
    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    private static readonly ResPath SnapshotDir = new("/character_snapshots");
    private const string Extension = ".yml";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
    }

    /// <summary>
    /// Captures <paramref name="body"/>'s current look and writes it for the character save <c>{userId}]{saveName}</c>.
    /// Call this alongside the character file save.
    /// </summary>
    public void SaveSnapshot(EntityUid body, NetUserId userId, string saveName)
    {
        try
        {
            var node = _serialization.WriteValueAs<MappingDataNode>(Capture(body), alwaysWrite: true);
            var path = GetPath(userId, saveName);

            _resourceManager.UserData.CreateDir(SnapshotDir);
            using var writer = _resourceManager.UserData.OpenWriteText(path);
            var stream = new YamlStream { new YamlDocument(node.ToYaml()) };
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }
        catch (Exception e)
        {
            // Cosmetic only, never block the actual save.
            Log.Error($"Failed to save character snapshot for {ToPrettyString(body)}: {e}");
        }
    }

    private CharacterAppearanceSnapshot Capture(EntityUid body)
    {
        var snapshot = new CharacterAppearanceSnapshot
        {
            Body = MetaData(body).EntityPrototype?.ID,
            CapturedAt = DateTime.UtcNow,
        };

        if (TryComp<HumanoidProfileComponent>(body, out var humanoid))
            snapshot.Species = humanoid.Species;

        if (_visualBody.TryGatherMarkingsData(body, null, out var profiles, out _, out var applied))
        {
            snapshot.OrganProfiles = profiles;
            snapshot.Markings = applied;
        }

        var slots = _inventory.GetSlotEnumerator(body);
        while (slots.NextItem(out var item, out var slot))
        {
            if (MetaData(item).EntityPrototype?.ID is { } id)
                snapshot.Slots[slot.Name] = id;
        }

        foreach (var hand in _hands.EnumerateHands(body))
        {
            if (_hands.TryGetHeldItem(body, hand, out var held, hideVirtualItems: true) &&
                MetaData(held.Value).EntityPrototype?.ID is { } id)
            {
                snapshot.Hands[hand] = id;
            }
        }

        return snapshot;
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent args)
    {
        var userId = args.PlayerSession.UserId;
        var prefix = $"{userId}]";
        var snapshots = new Dictionary<string, CharacterAppearanceSnapshot>();

        if (_resourceManager.UserData.IsDir(SnapshotDir))
        {
            foreach (var file in _resourceManager.UserData.DirectoryEntries(SnapshotDir))
            {
                if (!file.StartsWith(prefix) || !file.EndsWith(Extension))
                    continue;

                var saveName = file[prefix.Length..^Extension.Length];
                if (TryLoad(SnapshotDir / file, out var snapshot))
                    snapshots[saveName] = snapshot;
            }
        }

        // Always send, so a client never keeps a stale set from earlier in the session.
        RaiseNetworkEvent(new CharacterSnapshotsEvent(snapshots), args.PlayerSession);
    }

    private bool TryLoad(ResPath path, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out CharacterAppearanceSnapshot? snapshot)
    {
        snapshot = null;
        try
        {
            using var reader = _resourceManager.UserData.OpenText(path);
            var document = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();
            if (document == null)
                return false;

            snapshot = _serialization.Read<CharacterAppearanceSnapshot>(document.Root, notNullableOverride: true);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to read character snapshot {path}: {e.Message}");
            return false;
        }
    }

    private static ResPath GetPath(NetUserId userId, string saveName)
    {
        return SnapshotDir / $"{userId}]{saveName}{Extension}";
    }
}
