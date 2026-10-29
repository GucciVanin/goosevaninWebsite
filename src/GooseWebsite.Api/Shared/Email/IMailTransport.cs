namespace GooseWebsite.Api.Shared.Email;

/// <summary>
/// What a transport reports back to <see cref="MailService"/>. <paramref name="Diagnostics"/> holds only
/// log-safe categories (an HTTP status, an exception type name); transports never log and never put an
/// address, body, link, key or provider response text in it.
/// </summary>
public readonly record struct MailTransportOutcome(MailFailure Failure, string Diagnostics = "")
{
    public static MailTransportOutcome Accepted => new(MailFailure.None);

    public static MailTransportOutcome NotConfigured => new(MailFailure.NotConfigured);
}

/// <summary>A transport that delivers content the caller rendered.</summary>
public interface IRenderedMailTransport
{
    string Name { get; }

    Task<MailTransportOutcome> SendAsync(RenderedMail mail, CancellationToken cancellationToken);
}

/// <summary>A transport that asks the provider to render a stored template.</summary>
public interface ITemplateMailTransport
{
    string Name { get; }

    Task<MailTransportOutcome> SendTemplateAsync(TemplateMail mail, CancellationToken cancellationToken);
}
