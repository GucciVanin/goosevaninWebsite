using GooseWebsite.Api.Shared.Text;
using GooseWebsite.Api.Modules.Blog.Contracts;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Shared.Announcements;

namespace GooseWebsite.Api.Modules.Blog.Services;

// Owns the rules of authoring a story so the controller only maps outcomes to responses.
public sealed class BlogAuthoringService(IBlogPostRepository posts, IContentAnnouncer announcer, TimeProvider clock) : IBlogAuthoringService
{
    public async Task<AuthoringResult> CreateAsync(BlogPostRequest request, string authorId)
    {
        var errors = ValidateSave(request);
        if (errors.Count > 0)
        {
            return AuthoringResult.Invalid(errors);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var post = new BlogPost
        {
            Slug = await CreateUniqueSlugAsync(request.Title),
            AuthorId = authorId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsPublished = false
        };
        Apply(post, request);
        posts.Add(post);
        await posts.SaveChangesAsync();

        return AuthoringResult.Succeeded((await posts.GetByIdWithAuthorAsync(post.Id))!);
    }

    public async Task<AuthoringResult> UpdateAsync(int id, BlogPostRequest request)
    {
        var post = await posts.GetByIdWithAuthorAsync(id);
        if (post is null)
        {
            return AuthoringResult.NotFound();
        }

        var errors = ValidateSave(request);
        if (errors.Count > 0)
        {
            return AuthoringResult.Invalid(errors);
        }

        Apply(post, request);
        post.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await posts.SaveChangesAsync();
        return AuthoringResult.Succeeded(post);
    }

    public async Task<AuthoringResult> PublishAsync(int id)
    {
        var post = await posts.GetByIdWithAuthorAsync(id);
        if (post is null)
        {
            return AuthoringResult.NotFound();
        }

        if (post.IsPublished)
        {
            return AuthoringResult.Succeeded(post);
        }

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(post.Excerpt))
        {
            errors["excerpt"] = ["Add an excerpt before publishing."];
        }

        if (string.IsNullOrWhiteSpace(post.Content))
        {
            errors["content"] = ["Add the story text before publishing."];
        }

        if (post.CoverImage.Length > 0 && string.IsNullOrWhiteSpace(post.CoverImageAlt))
        {
            errors["coverImageAlt"] = ["Describe the cover image before publishing."];
        }

        if (errors.Count > 0)
        {
            return AuthoringResult.Invalid(errors);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        post.IsPublished = true;
        post.PublishedAtUtc ??= now;
        post.UpdatedAtUtc = now;

        // The first publish only: the marker is set once and never cleared, so unpublishing, editing and publishing
        // again tell nobody twice. These are two saves (the post, then the announcer's queue rows), but both run inside
        // the request's unit of work (UnitOfWorkFilter): nothing is committed until the action ends, and if the announcer
        // throws, the post stays a draft with no marker and no queue rows, and the request fails so the publish can be
        // repeated. The announcer sends no mail here, so a mail provider can never fail this request.
        var firstPublish = post.AnnouncedAtUtc is null;
        if (firstPublish)
        {
            post.AnnouncedAtUtc = now;
        }

        await posts.SaveChangesAsync();
        if (firstPublish)
        {
            await announcer.AnnounceAsync(new ContentAnnouncement(AnnouncedContentKind.Story, post.Id, post.Title, post.Excerpt, $"/stories/{post.Slug}"));
        }

        return AuthoringResult.Succeeded(post);
    }

    public async Task<AuthoringResult> UnpublishAsync(int id)
    {
        var post = await posts.GetByIdWithAuthorAsync(id);
        if (post is null)
        {
            return AuthoringResult.NotFound();
        }

        if (post.IsPublished)
        {
            post.IsPublished = false;
            post.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
            await posts.SaveChangesAsync();
        }

        return AuthoringResult.Succeeded(post);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var post = await posts.FindByIdAsync(id);
        if (post is null)
        {
            return false;
        }

        posts.Remove(post);
        await posts.SaveChangesAsync();
        return true;
    }

    private static Dictionary<string, string[]> ValidateSave(BlogPostRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["A story needs a title."];
        }

        if (!string.IsNullOrWhiteSpace(request.CoverImage) && !UploadedImageReference.IsValid(request.CoverImage.Trim()))
        {
            errors["coverImage"] = ["The cover image must be an image uploaded in the editor."];
        }

        if (request.Tags is not null && request.Tags.Any(tag => tag.Contains(',')))
        {
            errors["tags"] = ["A tag cannot contain a comma."];
        }

        return errors;
    }

    private void Apply(BlogPost post, BlogPostRequest request)
    {
        post.Title = request.Title.Trim();
        post.Excerpt = request.Excerpt.Trim();
        post.Content = request.Content;
        post.Category = string.IsNullOrWhiteSpace(request.Category) ? "Field notes" : request.Category.Trim();
        post.CoverImage = request.CoverImage.Trim();
        post.CoverImageAlt = post.CoverImage.Length == 0 ? string.Empty : request.CoverImageAlt.Trim();
        post.Tags = string.Join(',', CleanTags(request.Tags));
        if (request.PublishedAtUtc is { } publishedAt)
        {
            post.PublishedAtUtc = publishedAt.ToUniversalTime();
        }
    }

    // Trimmed, no empty tags, and no duplicates that differ only in letter case (the first spelling wins).
    private static List<string> CleanTags(IEnumerable<string>? tags) =>
        (tags ?? [])
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .DistinctBy(tag => tag.ToLowerInvariant())
            .ToList();

    private async Task<string> CreateUniqueSlugAsync(string title)
    {
        var baseSlug = Slugify(title);
        var slug = baseSlug;
        var suffix = 2;
        while (await posts.SlugExistsAsync(slug))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    internal static string Slugify(string title) => Slug.From(title, "story");
}
