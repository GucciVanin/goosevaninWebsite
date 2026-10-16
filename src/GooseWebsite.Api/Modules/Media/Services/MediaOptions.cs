namespace GooseWebsite.Api.Modules.Media.Services;

/// <summary>Where uploaded images are kept and how large one may be (5 MB, decided 2026-10-02).</summary>
public sealed class MediaOptions
{
    public const long MaxImageBytes = 5 * 1024 * 1024;

    /// <summary>The folder holding the files. In Docker it is on the data volume so images survive a restart.</summary>
    public string Directory { get; set; } = string.Empty;
}
