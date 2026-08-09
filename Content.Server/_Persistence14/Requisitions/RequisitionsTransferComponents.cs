namespace Content.Server._Persistence14.Requisitions;

/// <summary>
/// Runtime marker on a lathe: requisition prints it currently owes. When a finished item matches a job, the
/// console's in-progress count drops and (if the job is bound for a flatpacker) the board is moved into the
/// console's internal flatpack storage. If the lathe loses power mid-print, the outstanding jobs' contributed
/// materials are returned to the customer. Not persisted.
/// </summary>
[RegisterComponent]
public sealed partial class RequisitionsLatheJobComponent : Component
{
    public List<RequisitionJob> Jobs = new();
}

/// <summary>One in-progress requisition print on a lathe.</summary>
public struct RequisitionJob
{
    /// <summary>The recipe being printed (matched against finished items).</summary>
    public string Recipe;

    /// <summary>The console that took the order.</summary>
    public EntityUid Console;

    /// <summary>Materials the customer contributed toward this job, returned to them if the print fails.</summary>
    public Dictionary<string, int>? Cover;

    /// <summary>If set, the finished board is routed into the console's flatpack storage instead of delivered.</summary>
    public EntityUid? Flatpacker;
}
