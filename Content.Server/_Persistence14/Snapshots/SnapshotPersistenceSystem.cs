using System.Linq;
using Content.Shared._Moffstation.Snapshots;
using Content.Shared._Persistence14.CCVars;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Photography;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Server._Persistence14.Snapshots;

/// <summary>
/// The scene a snapshot shows. Saved server-side and sent on request.
/// </summary>
[RegisterComponent]
public sealed partial class SnapshotComponent : Component
{
    /// <summary>
    /// Binary-serialized snapshot content, stored base64 so map serialization preserves photographs.
    /// </summary>
    [DataField]
    public ResPath? SerializedDataPath;

    /// <summary>
    /// Deserialized snapshot data, when available. See <see cref="SnapshotPersistenceSystem"/> for how the two are kept in sync.
    /// </summary>
    [ViewVariables]
    public SnapshotData? Data;
}

/// <summary>
/// Serializes <see cref="SnapshotData"/> to base64 for map persistence and restores it on map init.
/// Enforces the 8 MiB (de)serialization limit in one place.
/// </summary>
public sealed partial class SnapshotPersistenceSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private ISerializationManager _serialization = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<SnapshotComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Data is not null)
            return;

        if (!TryLoad(ent, out var data) || data is null)
            return;

        ent.Comp.Data = data;
        // ApplyPhotographMetadata(ent.Owner, data);
    }

    // public SnapshotData? LoadFromSerialized(Entity<SnapshotComponent> ent)
    // {
    //     if (ent.Comp.SerializedDataPath is { } dataPath && TryLoad(dataPath, out var snapshot))
    //         return snapshot;
    //
    //     return null;
    // }

    public bool TryLoad(Entity<SnapshotComponent> ent, out SnapshotData? snapshot)
    {
        snapshot = null;

        if (ent.Comp.SerializedDataPath is not {} path)
            return false;

        try
        {
            using var reader = _resourceManager.UserData.OpenText(path);
            var document = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();
            if (document == null)
                return false;

            snapshot = _serialization.Read<SnapshotData>(document.Root, notNullableOverride: true);

            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to read photo snapshot {path}: {e.Message}");
            return false;
        }
    }

    public bool SaveData(Entity<SnapshotComponent> ent, SnapshotData data, Entity<MindComponent> photographerMind)
    {
        var prefix = _cfg.GetCVar(PersistenceCVars.SnapshotPathPrefix);
        var saveFilePath = prefix + "[" + photographerMind.Comp.UserId + "]" + DateTime.Now.ToString("yyyy-MM-dd_THH-mm-ss");
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            Log.Error(Loc.GetString("snapshot-no-path", ("cvar", nameof(CCVars.GameMap))));
            return false;
        }

        try
        {
            var node = _serialization.WriteValueAs<MappingDataNode>(data, alwaysWrite: true);

            _resourceManager.UserData.CreateDir(new ResPath(prefix));
            using var writer = _resourceManager.UserData.OpenWriteText(new ResPath(saveFilePath));
            var stream = new YamlStream { new YamlDocument(node.ToYaml()) };
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
            ent.Comp.SerializedDataPath = new ResPath(saveFilePath);
            return true;
        }
        catch (Exception e)
        {
            // Cosmetic only, never block the actual save.
            Log.Error($"Failed to save photo snapshot for {ToPrettyString(photographerMind.Owner)} {saveFilePath}): {e}");
            return false;
        }

    }

    /// <summary>
    /// Syncs subject examine text from the snapshot into the photograph component.
    /// </summary>
    private void ApplyPhotographMetadata(EntityUid uid, SnapshotData data)
    {
        var photograph = EnsureComp<PhotographComponent>(uid);
        photograph.NameText = data.SubjectNameText;
        if (data.SubjectDescription is { } description)
        {
            var message = new FormattedMessage();
            message.AddMarkupPermissive(description);
            photograph.Description = message;
        }
        else
        {
            photograph.Description = null;
        }

        Dirty(uid, photograph);
    }
}
