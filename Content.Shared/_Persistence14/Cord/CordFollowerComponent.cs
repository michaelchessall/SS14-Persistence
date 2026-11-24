using System.Numerics;
using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.PersistentIdentifier.Reference;
using Robust.Shared.GameStates;

namespace Content.Shared._Persistence14.Cord;

[RegisterComponent]
public sealed partial class CordFollowerComponent : Component
{
    [DataField]
    public PersistentEntityReference StartEntityTarget = PersistentIdentifierSystem.EmptyId;
    [DataField]
    public Vector2 StartOffset = Vector2.Zero;

    [DataField]
    public PersistentEntityReference EndEntityTarget = PersistentIdentifierSystem.EmptyId;
    [DataField]
    public Vector2 EndOffset = Vector2.Zero;
}