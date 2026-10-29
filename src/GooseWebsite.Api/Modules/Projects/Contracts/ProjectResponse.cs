namespace GooseWebsite.Api.Modules.Projects.Contracts;

public sealed record ProjectResponse(
    int Id,
    string Title,
    string Slug,
    string Overview,
    string Contribution,
    IReadOnlyList<string> Technologies,
    string? DemoUrl,
    string? SourceUrl,
    string CoverImage,
    string CoverImageAlt,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PublishedAtUtc);

// One row of the editor's workspace list: no body.
public sealed record ProjectSummaryResponse(
    int Id,
    string Title,
    string Slug,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PublishedAtUtc);

// What the public project pages need and nothing else: no database id and no editor state.
public sealed record PublicProjectResponse(
    string Title,
    string Slug,
    string Overview,
    string Contribution,
    IReadOnlyList<string> Technologies,
    string? DemoUrl,
    string? SourceUrl,
    string CoverImage,
    string CoverImageAlt,
    DateTime? PublishedAtUtc);
