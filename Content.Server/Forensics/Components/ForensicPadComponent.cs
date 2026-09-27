namespace Content.Server.Forensics
{
    /// <summary>
    /// Used to take a sample of someone's fingerprints.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ForensicPadComponent : Component
    {
        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;
        [DataField]
        public bool Used = false;
        [DataField]
        public String Sample = string.Empty;
    }
}
