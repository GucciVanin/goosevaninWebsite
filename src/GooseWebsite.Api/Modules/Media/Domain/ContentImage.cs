namespace GooseWebsite.Api.Modules.Media.Domain;

// An image Gustavo uploaded for a story or project. The file lives in the uploads folder under a generated name;
// this row records it so the editor can list and delete images.
public sealed class ContentImage
{
    public int Id { get; set; }

    /// <summary>The generated file name (32 hexadecimal characters and the extension). Never the name of the uploaded file.</summary>
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
