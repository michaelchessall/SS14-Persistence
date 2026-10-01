using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Telephone;

public abstract class SharedTelephoneSystem : EntitySystem
{
    public bool IsTelephoneEngaged(Entity<TelephoneComponent> entity)
    {
        return entity.Comp.LinkedTelephones.Any();
    }

    public string GetFormattedCallerIdForEntity(string? presumedName, string? presumedJob, Color fontColor, string fontType = "Default", int fontSize = 12)
    {
        var callerId = Loc.GetString("chat-telephone-unknown-caller",
            ("color", fontColor),
            ("fontType", fontType),
            ("fontSize", fontSize));

        if (presumedName == null)
            return callerId;

        if (presumedJob != null)
            callerId = Loc.GetString("chat-telephone-caller-id-with-job",
                ("callerName", FormattedMessage.RemoveMarkupPermissive(presumedName)),
                ("callerJob", FormattedMessage.RemoveMarkupPermissive(presumedJob)),
                ("color", fontColor),
                ("fontType", fontType),
                ("fontSize", fontSize));

        else
            callerId = Loc.GetString("chat-telephone-caller-id-without-job",
                ("callerName", FormattedMessage.RemoveMarkupPermissive(presumedName)),
                ("color", fontColor),
                ("fontType", fontType),
                ("fontSize", fontSize));

        return callerId;
    }

    public string GetFormattedDeviceIdForEntity(string? deviceName, Color fontColor, string fontType = "Default", int fontSize = 12)
    {
        if (deviceName == null)
        {
            return Loc.GetString("chat-telephone-unknown-device",
                ("color", fontColor),
                ("fontType", fontType),
                ("fontSize", fontSize));
        }

        return Loc.GetString("chat-telephone-device-id",
            ("deviceName", FormattedMessage.RemoveMarkupPermissive(deviceName)),
            ("color", fontColor),
            ("fontType", fontType),
            ("fontSize", fontSize));
    }

    // Persistence: Grid name in holopad caller ID
    public string GetFormattedGridNameForEntity(string? gridName, Color fontColor, string fontType = "Default", int fontSize = 12)
    {
        if (gridName == null)
        {
            return Loc.GetString("chat-telephone-unknown-grid",
                ("color", fontColor),
                ("fontType", fontType),
                ("fontSize", fontSize));
        }

        return Loc.GetString("chat-telephone-grid-name",
            ("gridName", FormattedMessage.RemoveMarkupPermissive(gridName)),
            ("color", fontColor),
            ("fontType", fontType),
            ("fontSize", fontSize));
    }
}
