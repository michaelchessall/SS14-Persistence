using System.Collections.Generic;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// A xenoarch trigger that activates when a matching item is "fed" to the artifact. An item passes if
/// its prototype is listed in <see cref="Foods"/> OR it matches the optional <see cref="Whitelist"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATFeedSystem)), AutoGenerateComponentState]
public sealed partial class XATFeedComponent : Component
{
    /// <summary>
    /// Specific item prototypes that satisfy the trigger, matched by prototype ID. This is how food
    /// triggers name individual dishes, since <see cref="EntityWhitelist"/> deliberately cannot match
    /// by prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> Foods = new();

    /// <summary>
    /// Optional tag/component whitelist. An item passes if it is in <see cref="Foods"/> OR matches this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// How long the feeding do-after takes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}

/// <summary> Do-after raised while feeding an item to an artifact with <see cref="XATFeedComponent"/>. </summary>
[Serializable, NetSerializable]
public sealed partial class XATFeedDoAfterEvent : DoAfterEvent
{
    public NetEntity Node;

    public XATFeedDoAfterEvent(NetEntity node)
    {
        Node = node;
    }

    public override DoAfterEvent Clone() => this;
}
