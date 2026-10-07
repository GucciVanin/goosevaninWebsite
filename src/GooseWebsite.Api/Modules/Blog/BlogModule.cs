using GooseWebsite.Api.Modules.Blog.Services;

namespace GooseWebsite.Api.Modules.Blog;

/// <summary>
/// Blog module: public published-post reads plus administrator-only authoring. The table mapping
/// is picked up automatically through <c>BlogPostConfiguration</c>.
/// </summary>
public static class BlogModule
{
    public static IServiceCollection AddBlogModule(this IServiceCollection services)
    {
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        return services;
    }
}
