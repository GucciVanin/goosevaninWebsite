using GooseWebsite.Api.Modules.Projects.Contracts;
using GooseWebsite.Api.Modules.Projects.Domain;
using GooseWebsite.Api.Shared.Announcements;
using GooseWebsite.Api.Shared.Text;

namespace GooseWebsite.Api.Modules.Projects.Services;

// Owns the rules of authoring a project so the controller only maps outcomes to responses.
public sealed class ProjectAuthoringService(IProjectRepository projects, IContentAnnouncer announcer, TimeProvider clock) : IProjectAuthoringService
{
    public async Task<ProjectResult> CreateAsync(ProjectRequest request, string authorId)
    {
        var errors = ValidateSave(request);
        if (errors.Count > 0)
        {
            return ProjectResult.Invalid(errors);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var project = new Project
        {
            Slug = await CreateUniqueSlugAsync(request.Title),
            AuthorId = authorId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsPublished = false
        };
        Apply(project, request);
        projects.Add(project);
        await projects.SaveChangesAsync();
        return ProjectResult.Succeeded(project);
    }

    public async Task<ProjectResult> UpdateAsync(int id, ProjectRequest request)
    {
        var project = await projects.GetByIdAsync(id);
        if (project is null)
        {
            return ProjectResult.NotFound();
        }

        var errors = ValidateSave(request);
        if (errors.Count > 0)
        {
            return ProjectResult.Invalid(errors);
        }

        Apply(project, request);
        project.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await projects.SaveChangesAsync();
        return ProjectResult.Succeeded(project);
    }

    public async Task<ProjectResult> PublishAsync(int id)
    {
        var project = await projects.GetByIdAsync(id);
        if (project is null)
        {
            return ProjectResult.NotFound();
        }

        if (project.IsPublished)
        {
            return ProjectResult.Succeeded(project);
        }

        if (string.IsNullOrWhiteSpace(project.Overview))
        {
            return ProjectResult.Invalid(new Dictionary<string, string[]> { ["overview"] = ["Add an overview before publishing."] });
        }

        if (project.CoverImage.Length > 0 && string.IsNullOrWhiteSpace(project.CoverImageAlt))
        {
            return ProjectResult.Invalid(new Dictionary<string, string[]> { ["coverImageAlt"] = ["Describe the cover image before publishing."] });
        }

        var now = clock.GetUtcNow().UtcDateTime;
        project.IsPublished = true;
        project.PublishedAtUtc ??= now;
        project.UpdatedAtUtc = now;

        // The first publish only (see BlogAuthoringService): the marker is set once and never cleared. The marker and
        // the announcer's queue rows are committed together by the request's unit of work, or not at all.
        var firstPublish = project.AnnouncedAtUtc is null;
        if (firstPublish)
        {
            project.AnnouncedAtUtc = now;
        }

        await projects.SaveChangesAsync();
        if (firstPublish)
        {
            await announcer.AnnounceAsync(new ContentAnnouncement(AnnouncedContentKind.Project, project.Id, project.Title, project.Overview, $"/projects/{project.Slug}"));
        }

        return ProjectResult.Succeeded(project);
    }

    public async Task<ProjectResult> UnpublishAsync(int id)
    {
        var project = await projects.GetByIdAsync(id);
        if (project is null)
        {
            return ProjectResult.NotFound();
        }

        if (project.IsPublished)
        {
            project.IsPublished = false;
            project.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
            await projects.SaveChangesAsync();
        }

        return ProjectResult.Succeeded(project);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await projects.GetByIdAsync(id);
        if (project is null)
        {
            return false;
        }

        projects.Remove(project);
        await projects.SaveChangesAsync();
        return true;
    }

    private static Dictionary<string, string[]> ValidateSave(ProjectRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["A project needs a title."];
        }

        if (request.Technologies is not null && request.Technologies.Any(item => item.Contains(',')))
        {
            errors["technologies"] = ["A technology cannot contain a comma."];
        }

        if (!string.IsNullOrWhiteSpace(request.CoverImage) && !UploadedImageReference.IsValid(request.CoverImage.Trim()))
        {
            errors["coverImage"] = ["The cover image must be an image uploaded in the editor."];
        }

        if (!IsAcceptableLink(request.DemoUrl))
        {
            errors["demoUrl"] = ["The demo link must be a web address starting with http:// or https://."];
        }

        if (!IsAcceptableLink(request.SourceUrl))
        {
            errors["sourceUrl"] = ["The source link must be a web address starting with http:// or https://."];
        }

        return errors;
    }

    // Empty is fine (the link is optional); otherwise only absolute http or https addresses, never javascript: and the like.
    private static bool IsAcceptableLink(string? link) =>
        string.IsNullOrWhiteSpace(link)
        || (Uri.TryCreate(link.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host));

    private static void Apply(Project project, ProjectRequest request)
    {
        project.Title = request.Title.Trim();
        project.Overview = request.Overview.Trim();
        project.Contribution = request.Contribution.Trim();
        project.CoverImage = request.CoverImage.Trim();
        project.CoverImageAlt = project.CoverImage.Length == 0 ? string.Empty : request.CoverImageAlt.Trim();
        project.Technologies = string.Join(',', CleanList(request.Technologies));
        project.DemoUrl = string.IsNullOrWhiteSpace(request.DemoUrl) ? null : request.DemoUrl.Trim();
        project.SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim();
        if (request.PublishedAtUtc is { } publishedAt)
        {
            project.PublishedAtUtc = publishedAt.ToUniversalTime();
        }
    }

    private static List<string> CleanList(IEnumerable<string>? items) =>
        (items ?? [])
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .DistinctBy(item => item.ToLowerInvariant())
            .ToList();

    private async Task<string> CreateUniqueSlugAsync(string title)
    {
        var baseSlug = Slug.From(title, "project");
        var slug = baseSlug;
        var suffix = 2;
        while (await projects.SlugExistsAsync(slug))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }
}
