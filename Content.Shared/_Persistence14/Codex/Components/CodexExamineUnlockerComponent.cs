using Content.Shared._Persistence14.Codex.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Codex.Components;

[RegisterComponent]
public sealed partial class CodexExamineUnlockerComponent : Component
{
    [DataField]
    public ProtoId<CodexEntryPrototype> Entry = "";

}
