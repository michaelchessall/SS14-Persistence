using Robust.Shared.GameStates;

namespace Content.Shared.CrewAssignments.Components;

[RegisterComponent]
public sealed partial class CrewAssignmentsComponent : Component
{
    [DataField]
    public Dictionary<int, CrewAssignment> CrewAssignments { get; set; } = new();
    [DataField]
    public int NextID = 1;
    public bool TryGetAssignment(int id, out CrewAssignment? assignment)
    {
        if (id == 0)
        {
            assignment = null;
            return false;
        }
        if (CrewAssignments.TryGetValue(id, out var currAssignment))
        {
            assignment = currAssignment;
            return true;
        }
        else
        {
            assignment = null;
            return false;
        }
    }
    public void CreateAssignment(string assignmentname, int wage = 0, int clevel = 0)
    {
        var id = NextID;
        NextID++;
        CrewAssignment newAssignment = new CrewAssignment(id, assignmentname, wage, clevel);
        CrewAssignments.Add(id, newAssignment);
    }
}


[DataDefinition]
[Serializable]
[Virtual]
public partial class CrewAssignment
{
    [DataField("_id")]
    public int ID = 0;
    [DataField("_name")]
    public string Name = "Unnamed Crew Assignment";
    [DataField("_wage")]
    public int Wage = 0;
    [DataField("_clevel")]
    public int Clevel = 0;
    [DataField("_accessids")]
    public List<string> AccessIDs = new();
    [DataField("_canAssign")]
    public bool CanAssign = false;
    [DataField("_canClaim")]
    public bool CanClaim = false;
    [DataField("_spendingLimit")]
    public int SpendingLimit = 0;
    [DataField("_canEditGeneralRecord")]
    public bool CanEditGeneralRecord = false;


    public CrewAssignment(int id, string name, int wage, int clevel)
    {
        ID = id;
        Name = name;
        Wage = wage;
        Clevel = clevel;
    }
}
