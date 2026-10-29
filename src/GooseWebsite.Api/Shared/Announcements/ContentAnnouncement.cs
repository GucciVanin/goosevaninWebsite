using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GooseWebsite.Api.Shared.Announcements;

/// <summary>The kind of public content that can be announced.</summary>
public enum AnnouncedContentKind
{
    Story,
    Project
}

/// <summary>
/// What a module tells the announcer when something is published for the first time. Only public information:
/// the module's own title, its summary text (a story's excerpt or a project's overview), and the site path of the
/// public page. The announcer is responsible for making the summary safe to put in an email.
/// </summary>
public sealed record ContentAnnouncement(AnnouncedContentKind Kind, int ContentId, string Title, string Summary, string PublicPath);

/// <summary>
/// The seam between publishing and the people who asked to hear about it (F10-US3). Blog and Projects call it after
/// the first publish; neither knows who listens, so neither depends on the Subscriptions module. The implementation
/// stages its work in the caller's unit of work and returns without sending anything: delivery is a background job.
/// </summary>
public interface IContentAnnouncer
{
    Task AnnounceAsync(ContentAnnouncement announcement, CancellationToken cancellationToken = default);
}

/// <summary>The announcer used when no module listens: it does nothing.</summary>
public sealed class NoContentAnnouncer : IContentAnnouncer
{
    public Task AnnounceAsync(ContentAnnouncement announcement, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public static class AnnouncementServiceCollectionExtensions
{
    /// <summary>
    /// Registers the quiet announcer unless a module already registered a real one. A listening module replaces it
    /// (<c>services.Replace</c>), so the order in which modules are added does not matter.
    /// </summary>
    public static IServiceCollection AddContentAnnouncerFallback(this IServiceCollection services)
    {
        services.TryAddScoped<IContentAnnouncer, NoContentAnnouncer>();
        return services;
    }
}
