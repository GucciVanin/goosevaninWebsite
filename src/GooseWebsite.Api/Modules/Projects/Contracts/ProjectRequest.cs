using System.ComponentModel.DataAnnotations;

namespace GooseWebsite.Api.Modules.Projects.Contracts;

// What Gustavo can save for a project. Saving never publishes: publishing is its own action. Only the title is
// required to save a draft; the overview is required to publish. Links are optional but must be web addresses.
public sealed class ProjectRequest
{
    [Required] public string Title { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string Contribution { get; set; } = string.Empty;
    public string[]? Technologies { get; set; }
    public string? DemoUrl { get; set; }
    public string? SourceUrl { get; set; }

    /// <summary>Optional: sets the publication date. The first publish sets it when it is empty.</summary>
    public DateTime? PublishedAtUtc { get; set; }
}
