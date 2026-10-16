using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Blog.Domain;

// Everybody reads published posts; drafts are visible to administrators only, and only administrators write.
// Gustavo is the only author, so the text has no length limit (it is still trimmed and cleaned of control characters).
public sealed class BlogPostAccessRule : EntityAccessRule<BlogPost>
{
    public BlogPostAccessRule() : base(WriteAccess.Administrator)
    {
        VisibleWhen(post => post.IsPublished);
        IsUserText(post => post.Title);
        IsUserText(post => post.Excerpt);
        IsUserText(post => post.Content);
        IsUserText(post => post.Category);
        IsUserText(post => post.Tags);
        IsUserText(post => post.CoverImageAlt);
    }
}
