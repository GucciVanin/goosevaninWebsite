using System.ComponentModel.DataAnnotations;

namespace GooseWebsite.Api.Modules.Blog.Contracts;

// Defines what Gustavo can save for a story. Saving never publishes: publishing is its own action, so there is
// no published flag here. Only the title is required to save a draft; the excerpt and body are required to publish.
// Gustavo is the only author, so the text has no length limit (decided with F7-US1, 2026-10-14).
public sealed class BlogPostRequest
{
    [Required] public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "Field notes";
    public string[]? Tags { get; set; }

    /// <summary>Optional: sets the publication date (for example to backdate an older story). The first publish sets it when it is empty.</summary>
    public DateTime? PublishedAtUtc { get; set; }
}
