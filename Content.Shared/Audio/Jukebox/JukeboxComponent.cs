using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared.Audio.Jukebox;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedJukeboxSystem))]
public sealed partial class JukeboxComponent : Component
{
    public const int MinVolumeLevel = 1;
    public const int MaxVolumeLevel = 10;
    public const int DefaultVolumeLevel = 7;

    [DataField, AutoNetworkedField]
    public ProtoId<JukeboxPrototype>? SelectedSongId;

    [DataField, AutoNetworkedField]
    public List<ProtoId<JukeboxPrototype>> Queue = new();

    [DataField, AutoNetworkedField]
    public bool ShuffleEnabled;

    [DataField, AutoNetworkedField]
    public bool RepeatSongEnabled;

    [DataField, AutoNetworkedField]
    public int VolumeLevel = DefaultVolumeLevel;

    [DataField, AutoNetworkedField]
    public EntityUid? AudioStream;

    /// <summary>
    /// RSI state for the jukebox being on.
    /// </summary>
    [DataField]
    public string? OnState;

    /// <summary>
    /// RSI state for the jukebox being on.
    /// </summary>
    [DataField]
    public string? OffState;

    /// <summary>
    /// RSI state for the jukebox track being selected.
    /// </summary>
    [DataField]
    public string? SelectState;

    [ViewVariables]
    public bool Selecting;

    [ViewVariables]
    public float SelectAccumulator;
}

[Serializable, NetSerializable]
public sealed class JukeboxPlayingMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxPauseMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxStopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxSelectedMessage(ProtoId<JukeboxPrototype> songId) : BoundUserInterfaceMessage
{
    public ProtoId<JukeboxPrototype> SongId { get; } = songId;
}

[Serializable, NetSerializable]
public sealed class JukeboxSetTimeMessage(float songTime) : BoundUserInterfaceMessage
{
    public float SongTime { get; } = songTime;
}

[Serializable, NetSerializable]
public sealed class JukeboxSetVolumeMessage(int volumeLevel) : BoundUserInterfaceMessage
{
    public int VolumeLevel { get; } = volumeLevel;
}

[Serializable, NetSerializable]
public sealed class JukeboxQueueSongMessage(ProtoId<JukeboxPrototype> songId) : BoundUserInterfaceMessage
{
    public ProtoId<JukeboxPrototype> SongId { get; } = songId;
}

[Serializable, NetSerializable]
public sealed class JukeboxClearQueueMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxSetShuffleMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public sealed class JukeboxSetRepeatSongMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public enum JukeboxVisuals : byte
{
    VisualState
}

[Serializable, NetSerializable]
public enum JukeboxVisualState : byte
{
    On,
    Off,
    Select,
}

public enum JukeboxVisualLayers : byte
{
    Base
}
