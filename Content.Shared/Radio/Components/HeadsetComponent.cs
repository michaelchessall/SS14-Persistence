using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radio.Components;

/// <summary>
/// This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeadsetComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool IsEquipped = false;

    [DataField, AutoNetworkedField]
    public SlotFlags RequiredSlot = SlotFlags.EARS;

    [DataField, AutoNetworkedField]
    public int TransmitTo = 0;

    [DataField, AutoNetworkedField]
    public int RecieveFrom = 0;

    /// <summary>
    /// Limits the range at which this headset can broadcast to a telecomm server
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxBroadcastRange = 512f;

}
[Serializable, NetSerializable]
public sealed class HeadsetMenuBoundUserInterfaceState : BoundUserInterfaceState
{
    public Dictionary<int, string> FormattedStations = new();
    public int TransmitTo = 0;
    public int RecieveFrom = 0;

    public HeadsetMenuBoundUserInterfaceState(Dictionary<int, string> formattedStations, int transmitTo, int recieveFrom)
    {
        FormattedStations = formattedStations;
        TransmitTo = transmitTo;
        RecieveFrom = recieveFrom;
    }
}
[Serializable, NetSerializable]
public enum HeadsetMenuUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class HeadsetMenuOutputSelect : BoundUserInterfaceMessage
{
    public int Target;
    public HeadsetMenuOutputSelect(int target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class HeadsetMenuInputSelect : BoundUserInterfaceMessage
{
    public int Target;
    public HeadsetMenuInputSelect(int target)
    {
        Target = target;
    }
}
