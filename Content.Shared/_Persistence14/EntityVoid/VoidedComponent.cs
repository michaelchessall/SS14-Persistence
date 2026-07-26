namespace Content.Shared._Persistence14.EntityVoid;

/// <summary>
/// A simple component identifying an entity as voided. Used in double-call protection attempts for the <see cref="EntityVoidSystem"/> 
/// </summary>
[RegisterComponent]
public sealed partial class VoidedComponent : Component { }