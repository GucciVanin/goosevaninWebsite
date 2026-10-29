using Microsoft.Extensions.DependencyInjection.Extensions;
using GooseWebsite.Api.Modules.Blog.Services;
using GooseWebsite.Api.Shared.Announcements;

namespace GooseWebsite.Api.Modules.Blog;

/// <summary>
/// Blog module: public published-post reads plus administrator-only authoring. The table mapping
/// is picked up automatically through <c>BlogPostConfiguration</c>.
/// </summary>
public static class BlogModule
{
    public static IServiceCollection AddBlogModule(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddContentAnnouncerFallback();
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        services.AddScoped<IBlogAuthoringService, BlogAuthoringService>();
        return services;
    }
}
