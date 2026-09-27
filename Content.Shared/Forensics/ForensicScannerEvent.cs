using Robust.Shared.Serialization;

namespace Content.Shared.Forensics
{
    [Serializable, NetSerializable]
    public sealed class ForensicScannerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly IEnumerable<string> Fingerprints = [];
        public readonly IEnumerable<string> Fibers = [];
        public readonly IEnumerable<string> TouchDNAs = [];
        public readonly IEnumerable<string> SolutionDNAs = [];
        public readonly IEnumerable<string> Residues = [];
        public readonly string LastScannedName = string.Empty;
        public readonly TimeSpan PrintCooldown = TimeSpan.Zero;
        public readonly TimeSpan PrintReadyAt = TimeSpan.Zero;

        public ForensicScannerBoundUserInterfaceState(
            IEnumerable<string> fingerprints,
            IEnumerable<string> fibers,
            IEnumerable<string> touchDnas,
            IEnumerable<string> solutionDnas,
            IEnumerable<string> residues,
            string lastScannedName,
            TimeSpan printCooldown,
            TimeSpan printReadyAt)
        {
            Fingerprints = fingerprints;
            Fibers = fibers;
            TouchDNAs = touchDnas;
            SolutionDNAs = solutionDnas;
            Residues = residues;
            LastScannedName = lastScannedName;
            PrintCooldown = printCooldown;
            PrintReadyAt = printReadyAt;
        }
    }

    [Serializable, NetSerializable]
    public enum ForensicScannerUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class ForensicScannerPrintMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ForensicScannerClearMessage : BoundUserInterfaceMessage
    {
    }
}
