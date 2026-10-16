namespace GooseWebsite.Api.Modules.Media.Services;

/// <summary>Decides what a file really is from its first bytes; the name and the declared type the browser sent are never trusted.</summary>
public static class ImageSniffer
{
    public sealed record Kind(string Extension, string ContentType);

    public static readonly Kind Jpeg = new("jpg", "image/jpeg");
    public static readonly Kind Png = new("png", "image/png");
    public static readonly Kind Webp = new("webp", "image/webp");
    public static readonly Kind Gif = new("gif", "image/gif");

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static Kind? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return Jpeg;
        }

        if (header.Length >= PngSignature.Length && header[..PngSignature.Length].SequenceEqual(PngSignature))
        {
            return Png;
        }

        // WebP is a RIFF container: "RIFF", four size bytes, then "WEBP".
        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return Webp;
        }

        // GIF files start with "GIF87a" or "GIF89a".
        if (header.Length >= 6 && (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8)))
        {
            return Gif;
        }

        return null;
    }
}
