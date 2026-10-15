using GooseWebsite.Api.Modules.Blog.Contracts;
using GooseWebsite.Api.Modules.Blog.Domain;

namespace GooseWebsite.Api.Modules.Blog.Services;

public enum AuthoringOutcome
{
    Succeeded,
    NotFound,

    /// <summary>The request breaks a rule; <see cref="AuthoringResult.Errors"/> says which field and why. Nothing was saved.</summary>
    Invalid
}

public sealed record AuthoringResult(AuthoringOutcome Outcome, BlogPost? Post = null, IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static AuthoringResult Succeeded(BlogPost post) => new(AuthoringOutcome.Succeeded, post);

    public static AuthoringResult NotFound() => new(AuthoringOutcome.NotFound);

    public static AuthoringResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(AuthoringOutcome.Invalid, Errors: errors);
}

/// <summary>
/// The Blog module's authoring interface (TE9). One place owns what makes a story valid and how it moves between
/// draft and published: the slug (fixed once created, so links stay stable), the tags, the timestamps, and the
/// rules for publishing. Saving never changes a story's state.
/// </summary>
public interface IBlogAuthoringService
{
    /// <summary>Creates a draft. The address (slug) is made from the title and made unique.</summary>
    Task<AuthoringResult> CreateAsync(BlogPostRequest request, string authorId);

    /// <summary>Saves edits. Does not publish, unpublish or change the slug.</summary>
    Task<AuthoringResult> UpdateAsync(int id, BlogPostRequest request);

    /// <summary>Publishes once: records the publication date if there is none; publishing again changes nothing.</summary>
    Task<AuthoringResult> PublishAsync(int id);

    /// <summary>Returns a published story to draft, keeping its publication date.</summary>
    Task<AuthoringResult> UnpublishAsync(int id);

    /// <summary>Deletes the story for good. Returns false when there is no such story.</summary>
    Task<bool> DeleteAsync(int id);
}
