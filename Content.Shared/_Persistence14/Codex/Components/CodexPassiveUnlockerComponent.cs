using Content.Shared._Persistence14.Codex.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Codex.Components;

[RegisterComponent]
public sealed partial class CodexPassiveUnlockerComponent : Component
{
    // int is the progress requirements. AVG 1 progress per 10 minutes.
    [DataField]
    public Dictionary<ProtoId<CodexEntryPrototype>, int> PossibleEntries = new();
    [DataField]
    public TimeSpan CheckCooldown = TimeSpan.FromMinutes(5);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CurrentCooldown = TimeSpan.Zero;
    [DataField]
    public ProtoId<CodexEntryPrototype> EntryToAdd = "";
    [DataField]
    public int ToAddRequirement = 0;

}
