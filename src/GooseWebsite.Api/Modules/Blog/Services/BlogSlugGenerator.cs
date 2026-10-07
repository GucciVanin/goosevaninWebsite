namespace GooseWebsite.Api.Modules.Blog.Services;

// Turns titles into URL slugs and resolves collisions with a numeric suffix.
public static class BlogSlugGenerator
{
    public static async Task<string> CreateUniqueAsync(string title, IBlogPostRepository repository)
    {
        var baseSlug = string.Join(
                '-',
                title.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Replace("?", string.Empty)
            .Replace("/", "-");

        var slug = baseSlug;
        var suffix = 2;
        while (await repository.SlugExistsAsync(slug))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }
}
