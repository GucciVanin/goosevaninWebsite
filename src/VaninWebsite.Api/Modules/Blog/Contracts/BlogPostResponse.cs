namespace VaninWebsite.Api.Modules.Blog.Contracts;

// Keeps public blog responses independent of persistence entities.
public sealed record BlogPostResponse(
    int Id,
    string Title,
    string Slug,
    string Excerpt,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string AuthorId,
    string AuthorName);