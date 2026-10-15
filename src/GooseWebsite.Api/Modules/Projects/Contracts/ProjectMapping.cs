using GooseWebsite.Api.Modules.Projects.Domain;

namespace GooseWebsite.Api.Modules.Projects.Contracts;

public static class ProjectMapping
{
    // SQLite hands dates back without a kind; stating UTC keeps every response written with a trailing Z.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? Utc(DateTime? value) => value is null ? null : Utc(value.Value);

    public static ProjectResponse ToResponse(Project project) => new(
        project.Id,
        project.Title,
        project.Slug,
        project.Overview,
        project.Contribution,
        project.Technologies.Split(',', StringSplitOptions.RemoveEmptyEntries),
        project.DemoUrl,
        project.SourceUrl,
        project.CoverImage,
        project.CoverImageAlt,
        project.IsPublished,
        Utc(project.CreatedAtUtc),
        Utc(project.UpdatedAtUtc),
        Utc(project.PublishedAtUtc));

    public static ProjectSummaryResponse ToSummary(Project project) => new(
        project.Id,
        project.Title,
        project.Slug,
        project.IsPublished,
        Utc(project.CreatedAtUtc),
        Utc(project.UpdatedAtUtc),
        Utc(project.PublishedAtUtc));
}
