using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.MessageBoard.Prototypes;

[Prototype]
public sealed partial class MessageBoardCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public string Name { get; set; } = string.Empty;
    [DataField]
    public int Order { get; set; } = 0;
}
