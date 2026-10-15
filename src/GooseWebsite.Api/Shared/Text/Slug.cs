using System.Globalization;
using System.Text;

namespace GooseWebsite.Api.Shared.Text;

// Addresses for stories and projects: "Same Title!" -> "same-title".
public static class Slug
{
    /// <summary>Letters and digits only, accents dropped, anything else becomes one hyphen; an empty result is the fallback.</summary>
    public static string From(string title, string fallback)
    {
        var slug = new StringBuilder();
        var lastWasHyphen = true;
        foreach (var character in title.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                slug.Append(char.ToLowerInvariant(character));
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                slug.Append('-');
                lastWasHyphen = true;
            }
        }

        var result = slug.ToString().Trim('-');
        return result.Length == 0 ? fallback : result;
    }
}
