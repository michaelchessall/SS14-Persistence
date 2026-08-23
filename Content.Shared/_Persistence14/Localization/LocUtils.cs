namespace Content.Shared._Persistence14.Localization;

public static class LocUtils
{
    /// <summary>
    /// Creates a concatenated "and" list of elements using Loc strings. <br/><br/>
    /// Ex. "apples, oranges, and bananas."
    /// </summary>
    public static string ConstructLocListAnd(params string[] elements)
        => ConstructLocList(ListType.And, "", null, elements);

    /// <summary>
    /// Creates a concatenated "and" list of elements using Loc strings. Logs invalid strings to the log manager.<br/><br/>
    /// Ex. "apples, oranges, and bananas."
    /// </summary>
    public static string ConstructLocListAnd(ILogManager log, params string[] elements)
        => ConstructLocList(ListType.And, "", log, elements);

    /// <summary>
    /// Creates a concatenated "or" list of elements using Loc strings. <br/><br/>
    /// Ex. "apples, oranges, or bananas."
    /// </summary>
    public static string ConstructLocListOr(params string[] elements)
        => ConstructLocList(ListType.Or, "", null, elements);
    /// <summary>
    /// Creates a concatenated "or" list of elements using Loc strings. Logs invalid strings to the log manager.<br/><br/>
    /// Ex. "apples, oranges, or bananas."
    /// </summary>
    public static string ConstructLocListOr(ILogManager log, params string[] elements)
        => ConstructLocList(ListType.Or, "", log, elements);

    /// <summary>
    /// Creates a concatenated list of elements using Loc strings and a custom connecting loc. <br/><br/>
    /// Ex. [<paramref name="customConnectorLoc"/>] = "then" => "apples, oranges, then bananas."
    /// </summary>
    public static string ConstructLocListCustom(string customConnectorLoc, params string[] elements)
        => ConstructLocList(ListType.Custom, customConnectorLoc, null, elements);
    /// <summary>
    /// Creates a concatenated list of elements using Loc strings and a custom connecting loc. Logs invalid strings to the log manager.<br/><br/>
    /// Ex. [<paramref name="customConnectorLoc"/>] = "then" => "apples, oranges, then bananas."
    /// </summary>
    public static string ConstructLocListCustom(ILogManager log, string customConnectorLoc, params string[] elements)
        => ConstructLocList(ListType.Custom, customConnectorLoc, log, elements);

    /// <summary>
    /// Creates a concatenated list of elements using Loc strings.
    /// Supports or and and lists implicitly, allows definition of custom connectors.
    /// </summary>
    private static string ConstructLocList(ListType type, string customConnectorLoc = "", ILogManager? log = null, params string[] elements)
    {
        if (elements.Length <= 0)
        {
            if (log is not null)
            {
                log.GetSawmill("localization-utils").Error("Unable to create list with 0 elements!");
            }
            return "INVALID";
        }

        if (elements.Length == 1)
            return elements[0];

        if (elements.Length == 2)
        {
            switch (type)
            {
                case ListType.And:
                    return Loc.GetString("list-two.and", ("a", elements[0]), ("b", elements[1]));
                case ListType.Or:
                    return Loc.GetString("list-two.or", ("a", elements[0]), ("b", elements[1]));
                default:
                    return Loc.GetString("list-two.custom", ("a", elements[0]), ("b", elements[1]), ("custom", Loc.GetString(customConnectorLoc)));
            }
        }

        var str = Loc.GetString("list-next", ("rest", elements[0]), ("next", elements[1]));
        for (int i = 2; i < elements.Length - 1; i++)
        {
            str = Loc.GetString("list-next", ("rest", str), ("next", elements[i]));
        }

        var last = elements[elements.Length - 1];
        switch (type)
        {
            case ListType.And:
                return Loc.GetString("list-last.and", ("rest", str), ("last", last));
            case ListType.Or:
                return Loc.GetString("list-last.or", ("rest", str), ("last", last));
            default:
                return Loc.GetString("list-last.custom", ("rest", str), ("last", last), ("custom", Loc.GetString(customConnectorLoc)));
        }
    }



    private enum ListType
    {
        And,
        Or,
        Custom
    }
}