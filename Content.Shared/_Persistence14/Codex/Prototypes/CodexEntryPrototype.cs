using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Codex.Prototypes;


[Prototype]
public sealed partial class CodexEntryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public string Name = "";
    [DataField]
    public string Content = "";
    [DataField]
    public ProtoId<CodexCategoryPrototype> Category = "";
    [DataField]
    public string UnlockHint = "";
    [DataField]
    public int Order = 0;
}
