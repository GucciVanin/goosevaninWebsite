using System.Text.RegularExpressions;

namespace GooseWebsite.Api.Shared.Text;

// How a story or project refers to an uploaded image: "/uploads/" and a generated file name. Anything else
// (another site, a data: address, a path with dots) is not an uploaded image and is refused.
public static partial class UploadedImageReference
{
    public const string UrlPrefix = "/uploads/";

    [GeneratedRegex(@"^/uploads/[0-9a-f]{32}\.(jpg|png|webp)$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? reference) => reference is not null && Pattern().IsMatch(reference);
}
