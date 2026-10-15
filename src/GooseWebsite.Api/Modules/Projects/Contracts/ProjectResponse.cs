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
