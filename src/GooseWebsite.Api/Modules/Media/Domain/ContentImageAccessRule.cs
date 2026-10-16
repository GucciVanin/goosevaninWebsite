using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Media.Domain;

// The list of uploaded images is for Gustavo only (the files themselves are public once a story or project uses them).
public sealed class ContentImageAccessRule : EntityAccessRule<ContentImage>
{
    public ContentImageAccessRule() : base(WriteAccess.Administrator)
    {
        VisibleWhen(_ => false);
    }
}
