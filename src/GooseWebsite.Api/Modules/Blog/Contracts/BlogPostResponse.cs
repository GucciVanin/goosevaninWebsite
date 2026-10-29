namespace GooseWebsite.Api.Modules.Blog.Contracts;

// Keeps story responses independent of persistence entities.
public sealed record BlogPostResponse(
    int Id,
    string Title,
    string Slug,
    string Excerpt,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    string CoverImage,
    string CoverImageAlt,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PublishedAtUtc,
    string AuthorId,
    string AuthorName);

// One row of the editor's workspace list: no body, so the list stays light.
public sealed record StorySummaryResponse(
    int Id,
    string Title,
    string Slug,
    string Category,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PublishedAtUtc);

// What the public story pages need and nothing else: no database id, no author account id, no editor state.
public sealed record PublicStoryResponse(
    string Title,
    string Slug,
    string Excerpt,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    string CoverImage,
    string CoverImageAlt,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc);
