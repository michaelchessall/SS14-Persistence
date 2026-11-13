namespace Content.Shared._Persistence14.Log;

public static class LogManagerExtensions
{
    private static bool InfoBoolean(ISawmill sawmill, bool value, string message, params object?[] args)
    {
        sawmill.Info(message, args);
        return value;
    }
    /// <summary>
    ///     A simple helper method that calls <see cref="ISawmill.Info(string, object?[])"/> and returns false.
    /// </summary>
    public static bool InfoFalse(this ISawmill sawmill, string message, params object?[] args)
        => InfoBoolean(sawmill, false, message, args);
    /// <summary>
    ///     A simple helper method that calls <see cref="ISawmill.Info(string, object?[])"/> and returns true.
    /// </summary>
    public static bool InfoTrue(this ISawmill sawmill, string message, params object?[] args)
        => InfoBoolean(sawmill, true, message, args);

    private static bool WarningBoolean(ISawmill sawmill, bool value, string message, params object?[] args)
    {
        sawmill.Warning(message, args);
        return value;
    }
    /// <summary>
    ///     A simple helper method that calls <see cref="ISawmill.Warning(string, object?[])"/> and returns false.
    /// </summary>
    public static bool WarningFalse(this ISawmill sawmill, string message, params object?[] args)
        => WarningBoolean(sawmill, false, message, args);
    /// <summary>
    ///     A simple helper method that calls <see cref="ISawmill.Warning(string, object?[])"/> and returns true.
    /// </summary>
    public static bool WarningTrue(this ISawmill sawmill, string message, params object?[] args)
        => WarningBoolean(sawmill, true, message, args);

    private static bool ErrorBoolean(ISawmill sawmill, bool value, string message, params object?[] args)
    {
        sawmill.Error(message, args);
        return value;
    }
    /// <summary>
    ///     A simple helper method that calls <see cref="ISawmill.Error(string, object?[])"/> and returns false.
    /// </summary>
    public static bool ErrorFalse(this ISawmill sawmill, string message, params object?[] args)
        => ErrorBoolean(sawmill, false, message, args);
    /// <summary>
    ///     A simple helper method that calls <see cref="ISawmill.Error(string, object?[])"/> and returns true.
    /// </summary>
    public static bool ErrorTrue(this ISawmill sawmill, string message, params object?[] args)
        => ErrorBoolean(sawmill, true, message, args);
}