namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed partial class CableVisComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("node", required: true)]
        public string Node;
    }

    // New for Persistence 14, allows junctions to function with multiple nodes while maintaining visuals.
    [RegisterComponent]
    public sealed partial class CableJunctionVisComponent : Component
    {
        [ViewVariables]
        [DataField("nodes", required: true)]
        public string[] Nodes;
    }
}
