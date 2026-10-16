using GooseWebsite.Api.Modules.Media.Domain;

namespace GooseWebsite.Api.Modules.Media.Services;

public enum ImageUploadOutcome
{
    Stored,

    /// <summary>Not a JPEG, PNG or WebP file, or empty.</summary>
    NotAnAllowedImage,

    TooLarge
}

public sealed record ImageUploadResult(ImageUploadOutcome Outcome, ContentImage? Image = null);

/// <summary>Stores and removes uploaded images. Nothing is written to disk unless the content is a real, small enough, allowed image.</summary>
public interface IImageService
{
    Task<ImageUploadResult> UploadAsync(Stream content, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentImage>> ListAsync();

    /// <summary>Removes the row and the file. Returns false when there is no such image.</summary>
    Task<bool> DeleteAsync(int id);
}
