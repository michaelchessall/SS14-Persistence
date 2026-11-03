using Content.Shared.Containers.ItemSlots;
using Content.Shared.GridControl.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.GridControl.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedGridConfigSystem))]
public sealed partial class StationTaggerComponent : Component
{
    public static string PrivilegedIdCardSlotId = "StationTagger-privilegedId";

    [DataField]
    public ItemSlot PrivilegedIdSlot = new();

    [DataField]
    public int? ConnectedStation = null;

    [DataField]
    public bool PersonalMode = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float DoAfter = 5f;

    public EntityUid TargetAccessReaderId = new();


}

[Serializable, NetSerializable]
public sealed class StationTaggerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly string TargetLabel;
    public readonly Color TargetLabelColor;
    public readonly string PrivilegedIdName;
    public readonly bool IsPrivilegedIdPresent;
    public readonly bool IsPrivilegedIdAuthorized;
    public string? StationName;
    public Dictionary<int, string>? PossibleStations = null;
    public int? TargetStation;
    public int? CurrentStation;

    public StationTaggerBoundUserInterfaceState(
            bool isPrivilegedIdPresent,
            bool isPrivilegedIdAuthorized,
            string privilegedIdName,
            string targetLabel,
            Color targetLabelColor,
            string? stationName,
            Dictionary<int, string> possibleStations,
            int? targetStation,
            int? currentStation)
    {
        IsPrivilegedIdPresent = isPrivilegedIdPresent;
        IsPrivilegedIdAuthorized = isPrivilegedIdAuthorized;
        PrivilegedIdName = privilegedIdName;
        TargetLabel = targetLabel;
        TargetLabelColor = targetLabelColor;
        StationName = stationName;
        PossibleStations = possibleStations;
        TargetStation = targetStation;
        CurrentStation = currentStation;
    }
}

[Serializable, NetSerializable]
public enum StationTaggerUiKey : byte
{
    Key,
}

[ByRefEvent, Serializable, NetSerializable]
public sealed class StationTaggedStateChangedEvent : EntityEventArgs
{
    public required NetEntity Target;
    public required NetEntity? Faction;
}

[Serializable, NetSerializable]
public sealed class StationTaggerLink : BoundUserInterfaceMessage
{
    public StationTaggerLink()
    {
    }
}

[Serializable, NetSerializable]
public sealed class StationTaggerUnlink : BoundUserInterfaceMessage
{
    public StationTaggerUnlink()
    {
    }
}



[Serializable, NetSerializable]
public sealed class StationTaggerTargetSelect : BoundUserInterfaceMessage
{
    public int Target;
    public StationTaggerTargetSelect(int target)
    {
        Target = target;
    }
}
