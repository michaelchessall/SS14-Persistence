using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.MessageBoard.Systems;

public abstract partial class SharedMessageBoardSystem : EntitySystem
{
}

[NetSerializable, Serializable]
public enum MessageBoardUiKey : byte
{
    Main
}
