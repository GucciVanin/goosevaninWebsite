using GooseWebsite.Api.Modules.Accounts.Domain;

namespace GooseWebsite.Api.Modules.Projects.Domain;

// A portfolio project Gustavo shows: what it is, what he did, with what, and where to see it. The overview is
// Markdown, shown safely, never as raw HTML.
public sealed class Project
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;

    /// <summary>What Gustavo did on it.</summary>
    public string Contribution { get; set; } = string.Empty;

    /// <summary>Comma separated; no entry contains a comma.</summary>
    public string Technologies { get; set; } = string.Empty;

    public string? DemoUrl { get; set; }
    public string? SourceUrl { get; set; }
    /// <summary>The cover image: an uploaded-image reference ("/uploads/<name>") or empty.</summary>
    public string CoverImage { get; set; } = string.Empty;

    /// <summary>What the cover image shows, for readers who cannot see it.</summary>
    public string CoverImageAlt { get; set; } = string.Empty;

    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When the project was first published; kept when it returns to draft.</summary>
    public DateTime? PublishedAtUtc { get; set; }

    /// <summary>When subscribers were first told about this project; set once, on the first publish, and never cleared.</summary>
    public DateTime? AnnouncedAtUtc { get; set; }

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;
}
