using System.Numerics;

namespace Content.Shared._Persistence14.Cord;

public abstract partial class SharedCordSystem : EntitySystem
{
    /// <summary>
    /// Retrieves the center of the beam.
    /// </summary>
    public Vector2 GetBeamMidPoint(CordComponent beam)
    {
        return (beam.EndPosition + beam.StartPosition) / 2f;
    }
}