using GooseWebsite.Api.Modules.Blog.Domain;

namespace GooseWebsite.Api.Modules.Blog.Contracts;

// One place that turns a stored story into the shapes the API returns.
public static class BlogPostMapping
{
    // SQLite hands dates back without a kind; stating UTC keeps every response written with a trailing Z.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? Utc(DateTime? value) => value is null ? null : Utc(value.Value);

    public static BlogPostResponse ToResponse(BlogPost post) => new(
        post.Id,
        post.Title,
        post.Slug,
        post.Excerpt,
        post.Content,
        post.Category,
        post.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries),
        post.CoverImage,
        post.CoverImageAlt,
        post.IsPublished,
        Utc(post.CreatedAtUtc),
        Utc(post.UpdatedAtUtc),
        Utc(post.PublishedAtUtc),
        post.AuthorId,
        post.Author.DisplayName);

    public static StorySummaryResponse ToSummary(BlogPost post) => new(
        post.Id,
        post.Title,
        post.Slug,
        post.Category,
        post.IsPublished,
        Utc(post.CreatedAtUtc),
        Utc(post.UpdatedAtUtc),
        Utc(post.PublishedAtUtc));
}
