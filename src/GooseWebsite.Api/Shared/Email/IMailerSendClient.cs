namespace GooseWebsite.Api.Shared.Email;

/// <summary>A recipient of a templated email.</summary>
public sealed record MailerSendRecipient(string Email, string Name);

/// <summary>
/// An email rendered by a template stored at MailerSend. <paramref name="Variables"/> must contain every
/// variable the template uses (names are case-sensitive); a missing one renders as the variable name.
/// </summary>
public sealed record MailerSendTemplateEmail(
    string TemplateId,
    string Subject,
    MailerSendRecipient To,
    IReadOnlyDictionary<string, string> Variables,
    MailerSendRecipient? ReplyTo = null);

/// <summary>
/// The only place the application talks to the MailerSend HTTP API. Modules choose the template and
/// supply its variables; they never build HTTP requests.
/// </summary>
public interface IMailerSendClient
{
    /// <summary>True when an API key and sender address are configured.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends one templated email. Returns false (and logs only the HTTP status code) when the API is not
    /// configured or the request fails.
    /// </summary>
    Task<bool> SendTemplateAsync(MailerSendTemplateEmail email, CancellationToken cancellationToken = default);
}
