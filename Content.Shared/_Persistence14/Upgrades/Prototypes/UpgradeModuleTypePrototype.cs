using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Persistence14.Upgrades.Prototypes;

[Prototype]
public sealed partial class UpgradeModuleTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public string Name { get; set; } = string.Empty;
}
