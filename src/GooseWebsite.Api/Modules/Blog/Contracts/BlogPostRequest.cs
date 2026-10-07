using System.ComponentModel.DataAnnotations;

namespace GooseWebsite.Api.Modules.Blog.Contracts;

// Defines the validated content boundary for blog post changes.
public sealed class BlogPostRequest
{
    [Required, StringLength(160)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(280)] public string Excerpt { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    [StringLength(80)] public string Category { get; set; } = "Field notes";
    public string[]? Tags { get; set; }
    public bool IsPublished { get; set; }
}