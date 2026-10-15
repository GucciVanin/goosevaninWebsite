using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Media.Domain;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Media.Services;

public sealed class ImageService(DatabaseService database, IOptions<MediaOptions> options, TimeProvider clock) : IImageService
{
    public async Task<ImageUploadResult> UploadAsync(Stream content, CancellationToken cancellationToken)
    {
        // Read in chunks and stop as soon as the limit is passed, so an oversized upload is never buffered whole.
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MediaOptions.MaxImageBytes)
            {
                return new ImageUploadResult(ImageUploadOutcome.TooLarge);
            }

            buffer.Write(chunk, 0, read);
        }

        var bytes = buffer.ToArray();
        var kind = ImageSniffer.Detect(bytes);
        if (kind is null)
        {
            return new ImageUploadResult(ImageUploadOutcome.NotAnAllowedImage);
        }

        var fileName = $"{Guid.NewGuid():N}.{kind.Extension}";
        var path = PathFor(fileName);
        Directory.CreateDirectory(options.Value.Directory);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);

        var image = new ContentImage
        {
            FileName = fileName,
            ContentType = kind.ContentType,
            SizeBytes = bytes.LongLength,
            UploadedAtUtc = clock.GetUtcNow().UtcDateTime
        };
        try
        {
            database.Add(image);
            await database.SaveChangesAsync();
        }
        catch
        {
            File.Delete(path);
            throw;
        }

        return new ImageUploadResult(ImageUploadOutcome.Stored, image);
    }

    public async Task<IReadOnlyList<ContentImage>> ListAsync() =>
        await database.Read<ContentImage>().OrderByDescending(image => image.UploadedAtUtc).ToListAsync();

    public async Task<bool> DeleteAsync(int id)
    {
        var image = await database.ReadForUpdate<ContentImage>().SingleOrDefaultAsync(item => item.Id == id);
        if (image is null)
        {
            return false;
        }

        database.Remove(image);
        await database.SaveChangesAsync();
        File.Delete(PathFor(image.FileName));
        return true;
    }

    // File names come from this service only (32 hexadecimal characters and an extension), never from a request.
    private string PathFor(string fileName) => Path.Combine(options.Value.Directory, fileName);
}
