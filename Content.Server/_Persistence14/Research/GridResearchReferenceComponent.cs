using Content.Shared._Persistence14.PersistentIdentifier;
using Content.Shared._Persistence14.PersistentIdentifier.Reference;

namespace Content.Shared._Persistence14.Research;

[RegisterComponent]
public sealed partial class GridResearchReferenceComponent : Component
{
    [DataField]
    public Dictionary<PersistentEntityReference, PersistentEntityReference> ServerAuthoritiesByFaction = new();
}