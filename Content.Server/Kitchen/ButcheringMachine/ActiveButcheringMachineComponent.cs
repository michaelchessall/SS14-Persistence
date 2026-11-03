using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.ButcheringMachine
{
    [RegisterComponent]
    public sealed partial class ActiveButcheringMachineComponent : Component
    {
        [DataField]
        public SoundSpecifier SoundEffect = new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg");
    }
}
