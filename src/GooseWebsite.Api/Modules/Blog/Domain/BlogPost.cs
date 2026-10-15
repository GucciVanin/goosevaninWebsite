using GooseWebsite.Api.Modules.Accounts.Domain;

namespace GooseWebsite.Api.Modules.Blog.Domain;

// Stores a story (the product name; the code keeps its historical "blog post" name) and its author relationship
// independently of API contracts. Gustavo writes the body in Markdown; it is shown safely, never as raw HTML.
public sealed class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "Field notes";
    public string Tags { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When the story was first published; Gustavo may change it (for example to backdate an older story). Kept when the story returns to draft.</summary>
    public DateTime? PublishedAtUtc { get; set; }

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;
}
