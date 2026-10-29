using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Turns text that Gustavo (or a visitor) wrote for the public site into text that is safe to put in an email: one
// plain-text value, no markup, no control or invisible characters that could hide or reorder text or reach a mail
// header, and a bounded length. The HTML view of a mail still HTML-encodes whatever this returns.
internal static partial class PublicText
{
    /// <summary>Removes control characters, line and paragraph separators (Zl, Zp) and format characters (Cf: bidirectional overrides, zero-width marks, tag characters).</summary>
    public static string StripHidden(string? text) =>
        string.Concat((text ?? string.Empty).EnumerateRunes().Where(rune => !IsHidden(rune)).Select(rune => rune.ToString()));

    /// <summary>One line: hidden characters removed, every run of whitespace (line breaks included) becomes one space.</summary>
    public static string OneLine(string? text)
    {
        var builder = new StringBuilder();
        foreach (var rune in (text ?? string.Empty).EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category == UnicodeCategory.Format)
            {
                continue;
            }

            if (Rune.IsWhiteSpace(rune) || Rune.IsControl(rune) || category is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
            {
                builder.Append(' ');
            }
            else
            {
                builder.Append(rune.ToString());
            }
        }

        return Spaces().Replace(builder.ToString(), " ").Trim();
    }

    /// <summary>A short plain-text title: one line, cut with an ellipsis.</summary>
    public static string Title(string? text, int maxLength) => Shorten(OneLine(text), maxLength);

    /// <summary>
    /// The text of a story excerpt or a project overview (Markdown on the site) as one short plain paragraph:
    /// links and images keep only their words, markers disappear, and nothing in it is a link or markup.
    /// </summary>
    public static string Summary(string? markdown, int maxLength)
    {
        var text = markdown ?? string.Empty;
        text = Fence().Replace(text, string.Empty);
        text = Image().Replace(text, "$1");
        text = Link().Replace(text, "$1");
        text = LineMarker().Replace(text, string.Empty);
        text = Emphasis().Replace(text, string.Empty);
        return Shorten(OneLine(text), maxLength);
    }

    private static bool IsHidden(Rune rune) =>
        Rune.IsControl(rune)
        || Rune.GetUnicodeCategory(rune) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.Format;

    // Cut at a word boundary when one is near the end, never inside a surrogate pair, and say so with an ellipsis.
    private static string Shorten(string text, int maxLength)
    {
        maxLength = Math.Max(2, maxLength);
        if (text.Length <= maxLength)
        {
            return text;
        }

        var cut = maxLength - 1;
        if (char.IsHighSurrogate(text[cut - 1]))
        {
            cut--;
        }

        var space = text.LastIndexOf(' ', cut - 1, cut);
        if (space >= cut * 6 / 10)
        {
            cut = space;
        }

        return text[..cut].TrimEnd() + "…";
    }

    [GeneratedRegex(@"[ ]{2,}")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"^[ \t]*(```|~~~).*$", RegexOptions.Multiline)]
    private static partial Regex Fence();

    [GeneratedRegex(@"!\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex Image();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"^[ \t]*(#{1,6}[ \t]*|>+[ \t]?|[-*+][ \t]+|\d+[.)][ \t]+)", RegexOptions.Multiline)]
    private static partial Regex LineMarker();

    [GeneratedRegex(@"\*\*|__|~~|`|\*")]
    private static partial Regex Emphasis();
}
