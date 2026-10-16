using GooseWebsite.Api.Modules.Media.Domain;
using GooseWebsite.Api.Shared.Text;

namespace GooseWebsite.Api.Modules.Media.Contracts;

/// <summary>What the editor gets for an uploaded image: the reference to store on a story or project, and to use in Markdown.</summary>
public sealed record ImageResponse(int Id, string Url, string ContentType, long SizeBytes, DateTime UploadedAtUtc)
{
    public static ImageResponse From(ContentImage image) => new(
        image.Id,
        UploadedImageReference.UrlPrefix + image.FileName,
        image.ContentType,
        image.SizeBytes,
        DateTime.SpecifyKind(image.UploadedAtUtc, DateTimeKind.Utc));
}
