using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XATItemInteractComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Whitelist = new();

    [DataField, AutoNetworkedField]
    public TimeSpan UseTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public int ReduceStackBy = 0;
}

[NetSerializable, Serializable]
public sealed partial class XATItemInteractDoAfterEvent : DoAfterEvent
{
    public NetEntity Node;

    public XATItemInteractDoAfterEvent(NetEntity node)
    {
        Node = node;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}