using Content.Shared._Scav.Speech.EntitySystems;
using Content.Shared.Speech.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Scav.Speech.Components;

/// <summary>
/// Dental Frictives!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(YingletAccentSystem))]
public sealed partial class YingletAccentComponent : BaseAccentComponent;
