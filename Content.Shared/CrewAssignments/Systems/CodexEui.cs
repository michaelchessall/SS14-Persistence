using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.CrewAssignments.Systems;

[Serializable, NetSerializable]
public sealed class CodexEuiState : EuiStateBase
{
    public List<CodexEntry> Entries { get; }
    public string SectorStatus { get; }

    public CodexEuiState(List<CodexEntry> entries, string sectorStatus)
    {
        Entries = entries;
        SectorStatus = sectorStatus;
    }
}
[DataDefinition, NetSerializable, Serializable]
[Virtual]
public partial class CodexEntry
{
    [DataField("_id")]
    public int ID = 0;
    [DataField]
    public string Title { get; set; } = "Unnamed Codex Entry";
    [DataField]
    public string Description { get; set; } = "";

    [DataField]
    public bool Visible = false;

    [DataField]
    public List<string> Whitelist { get; set; } = new();

    public CodexEntry(int id)
    {
        ID = id;
    }
}

public static class CodexEuiMsg
{

    [Serializable, NetSerializable]
    public sealed class DeleteWhitelist : EuiMessageBase
    {
        public DeleteWhitelist(int iD, string name)
        {
            ID = iD;
            Name = name;
        }
        public int ID { get; }
        public string Name { get; }
    }

    [Serializable, NetSerializable]
    public sealed class SaveWhitelist : EuiMessageBase
    {
        public SaveWhitelist(int iD, string name)
        {
            ID = iD;
            Name = name;
        }
        public int ID { get; }
        public string Name { get; }
    }
    [Serializable, NetSerializable]
    public sealed class Select : EuiMessageBase
    {
        public Select(int iD)
        {
            ID = iD;
        }
        public int ID { get; }
    }
    [Serializable, NetSerializable]
    public sealed class Delete : EuiMessageBase
    {
        public Delete(int iD)
        {
            ID = iD;
        }
        public int ID { get; }
    }
    [Serializable, NetSerializable]
    public sealed class Save : EuiMessageBase
    {
        public Save(int iD, string title, string description)
        {
            ID = iD;
            Title = title;
            Description = description;
        }

        public int ID { get; }
        public string Title { get; }
        public string Description { get; }


    }
    [Serializable, NetSerializable]
    public sealed class Reveal : EuiMessageBase
    {
        public Reveal(int iD)
        {
            ID = iD;
        }

        public int ID { get; }


    }
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class CreateNew : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class SectorButton : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class SaveChangesCompleted : EuiMessageBase
    {
        public int ID { get; }
        public string Title { get; }
        public string Description { get; }
        public string? Reward { get; }
        public string? CompletedDescription { get; }

        public SaveChangesCompleted(int iD, string title, string description, string? reward, string? completedDescription)
        {
            ID = iD;
            Title = title;
            Description = description;
            Reward = reward;
            CompletedDescription = completedDescription;
        }
    }
    [Serializable, NetSerializable]
    public sealed class SaveChanges : EuiMessageBase
    {
        public int ID { get; }
        public string Title { get; }
        public string Description { get; }
        public string? Reward { get; }
        public string? CompletedDescription { get; }

        public SaveChanges(int iD, string title, string description, string? reward, string? completedDescription)
        {
            ID = iD;
            Title = title;
            Description = description;
            Reward = reward;
            CompletedDescription = completedDescription;
        }
    }
    [Serializable, NetSerializable]
    public sealed class Follow : EuiMessageBase
    {
        public NetEntity TargetWorldObjectives { get; }

        public Follow(NetEntity targetWorldObjectives)
        {
            TargetWorldObjectives = targetWorldObjectives;
        }
    }

    [Serializable, NetSerializable]
    public sealed class Send : EuiMessageBase
    {
        public NetEntity Target { get; }
        public string Title { get; }
        public string From { get; }
        public string Content { get; }
        public string StampState { get; }
        public Color StampColor { get; }
        public bool Locked { get; }

        public Send(NetEntity target, string title, string from, string content, string stamp, Color stampColor, bool locked)
        {
            Target = target;
            Title = title;
            From = from;
            Content = content;
            StampState = stamp;
            StampColor = stampColor;
            Locked = locked;
        }
    }
}
