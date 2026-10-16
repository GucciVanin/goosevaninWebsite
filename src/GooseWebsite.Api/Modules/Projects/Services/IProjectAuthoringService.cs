using GooseWebsite.Api.Modules.Projects.Contracts;
using GooseWebsite.Api.Modules.Projects.Domain;

namespace GooseWebsite.Api.Modules.Projects.Services;

public enum ProjectOutcome
{
    Succeeded,
    NotFound,

    /// <summary>The request breaks a rule; <see cref="ProjectResult.Errors"/> says which field and why. Nothing was saved.</summary>
    Invalid
}

public sealed record ProjectResult(ProjectOutcome Outcome, Project? Project = null, IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static ProjectResult Succeeded(Project project) => new(ProjectOutcome.Succeeded, project);

    public static ProjectResult NotFound() => new(ProjectOutcome.NotFound);

    public static ProjectResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(ProjectOutcome.Invalid, Errors: errors);
}

/// <summary>
/// The Projects module's authoring interface. It owns what makes a project valid (title, web addresses, technologies),
/// the slug (fixed once created) and how a project moves between draft and published. Saving never changes the state.
/// </summary>
public interface IProjectAuthoringService
{
    Task<ProjectResult> CreateAsync(ProjectRequest request, string authorId);

    Task<ProjectResult> UpdateAsync(int id, ProjectRequest request);

    /// <summary>Publishes once: needs an overview; records the publication date if there is none; publishing again changes nothing.</summary>
    Task<ProjectResult> PublishAsync(int id);

    Task<ProjectResult> UnpublishAsync(int id);

    Task<bool> DeleteAsync(int id);
}
