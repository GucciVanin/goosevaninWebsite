using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Projects.Domain;

// Everybody reads published projects; drafts are visible to administrators only, and only administrators write.
// Gustavo is the only author, so the text has no length limit (it is still trimmed and cleaned of control characters).
public sealed class ProjectAccessRule : EntityAccessRule<Project>
{
    public ProjectAccessRule() : base(WriteAccess.Administrator)
    {
        VisibleWhen(project => project.IsPublished);
        IsUserText(project => project.Title);
        IsUserText(project => project.Overview);
        IsUserText(project => project.Contribution);
        IsUserText(project => project.Technologies);
        IsUserText(project => project.CoverImageAlt);
    }
}
