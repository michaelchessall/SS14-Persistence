namespace Content.Shared.Paper;

/// <summary>
/// Shared utility methods for paper form tag processing.
/// </summary>
public static class PaperTagUtility
{
    /// <summary>
    /// Replaces the nth occurrence of a tag with replacement text.
    /// </summary>
    public static string ReplaceNthTag(string text, string tag, int index, string replacement)
    {
        var currentIndex = 0;
        var pos = 0;

        while (pos < text.Length)
        {
            var foundPos = text.IndexOf(tag, pos, StringComparison.Ordinal);
            if (foundPos == -1)
                break;

            if (currentIndex == index)
                return string.Concat(text.AsSpan(0, foundPos), replacement, text.AsSpan(foundPos + tag.Length));

            currentIndex++;
            pos = foundPos + tag.Length;
        }

        return text;
    }

    /// <summary>
    /// Replaces all instances of a tag in the text with a given string.
    /// </summary>
    public static string ReplaceAllTag(string text, string tag, string replacement)
    {
        return text.Replace(tag, replacement); // This proved far simpler than I thought it would be...
    }

    /// <summary>
    /// Removes any unfilled [form] and [signature] tags, and converts [check] tags to ☐.
    /// Called when the paper is stamped to finalize the document.
    /// </summary>
    public static string CleanUnfilledTags(string text) =>
        text.Replace("[form]", string.Empty)
            .Replace("[signature]", string.Empty)
            .Replace("[check]", "☐");
}
