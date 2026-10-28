namespace GooseWebsite.Api.Shared.Email;

/// <summary>
/// The only mail interface modules use. They say what to send; the transport (SMTP or MailerSend), the
/// failure model and the logging rule live behind it. Never throws: failures come back as a result.
/// </summary>
public interface IMailService
{
    /// <summary>Sends an email whose content the caller rendered (SMTP transport).</summary>
    Task<MailResult> SendAsync(RenderedMail mail, CancellationToken cancellationToken = default);

    /// <summary>Sends an email rendered by a provider template (MailerSend transport).</summary>
    Task<MailResult> SendTemplateAsync(TemplateMail mail, CancellationToken cancellationToken = default);
}
