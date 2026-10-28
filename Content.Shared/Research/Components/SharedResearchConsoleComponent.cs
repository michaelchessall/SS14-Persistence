using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
    {
        public string Id;

        public ConsoleUnlockTechnologyMessage(string id)
        {
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public required int Points;
        public required float CostMultiplier;

        public required HashSet<ProtoId<TechnologyPrototype>> AvailableTechnologies = new();
        public required HashSet<ProtoId<TechnologyPrototype>> UnlockedTechnologies = new();
        public required HashSet<ProtoId<TechDisciplinePrototype>> SupportedDisciplines = new();
    }
}
