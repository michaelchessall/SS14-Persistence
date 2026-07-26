using Robust.Shared.GameStates;

namespace Content.Shared._Persistence14.PersistentIdentifier;

/// <summary>
/// A component allowing reference to the entity this is attached to between runtimes through a serialized GUID.
/// </summary>
[RegisterComponent, Access(typeof(PersistentIdentifierSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PersistentIdentifierComponent : Component
{
    /// <summary>
    /// A serialized identifier used to reference the entity this component is attached to between runtimes.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public string Id = PersistentIdentifierSystem.EmptyId;

    /// <summary>
    /// Shows the current initialization state of the ID and allows for basic manipulation of that ID.<br/><br/>
    /// Returns true when the ID has been initialized and is not null.<br/>
    /// Returns false when the ID is null.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IdInit => Id != PersistentIdentifierSystem.EmptyId;
}