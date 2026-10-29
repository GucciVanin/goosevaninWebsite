using Microsoft.Extensions.DependencyInjection.Extensions;
using GooseWebsite.Api.Modules.Projects.Services;
using GooseWebsite.Api.Shared.Announcements;

namespace GooseWebsite.Api.Modules.Projects;

/// <summary>
/// Projects module: public published-project reads plus administrator-only authoring. The table mapping is
/// picked up automatically through <c>ProjectConfiguration</c>.
/// </summary>
public static class ProjectsModule
{
    public static IServiceCollection AddProjectsModule(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddContentAnnouncerFallback();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectAuthoringService, ProjectAuthoringService>();
        return services;
    }
}
