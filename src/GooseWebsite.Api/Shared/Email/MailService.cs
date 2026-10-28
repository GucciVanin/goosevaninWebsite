namespace GooseWebsite.Api.Shared.Email;

/// <summary>
/// The one mail service. It picks the transport for each kind of mail, turns every outcome (including an
/// exception) into a <see cref="MailResult"/>, and is the single place mail is logged. The privacy rule
/// lives here: log lines carry categories only (transport, kind, failure, HTTP or SMTP status, exception
/// type), never an address, name, subject, body, link, token, key or provider response text.
/// </summary>
public sealed class MailService(
    IRenderedMailTransport renderedTransport,
    ITemplateMailTransport templateTransport,
    ILogger<MailService> logger) : IMailService
{
    public async Task<MailResult> SendAsync(RenderedMail mail, CancellationToken cancellationToken = default)
    {
        MailTransportOutcome outcome;
        try
        {
            outcome = await renderedTransport.SendAsync(mail, cancellationToken);
        }
        catch (Exception exception)
        {
            outcome = new MailTransportOutcome(MailFailure.TransportError, $"exceptionType={exception.GetType().Name}");
        }

        return Report("rendered", renderedTransport.Name, outcome);
    }

    public async Task<MailResult> SendTemplateAsync(TemplateMail mail, CancellationToken cancellationToken = default)
    {
        MailTransportOutcome outcome;
        try
        {
            outcome = await templateTransport.SendTemplateAsync(mail, cancellationToken);
        }
        catch (Exception exception)
        {
            outcome = new MailTransportOutcome(MailFailure.TransportError, $"templateId={mail.TemplateId}; exceptionType={exception.GetType().Name}");
        }

        return Report("template", templateTransport.Name, outcome);
    }

    private MailResult Report(string kind, string transport, MailTransportOutcome outcome)
    {
        switch (outcome.Failure)
        {
            case MailFailure.None:
                logger.LogInformation("Mail delivery succeeded; kind={Kind}; transport={Transport}; {Diagnostics}.", kind, transport, outcome.Diagnostics);
                return MailResult.Success;
            case MailFailure.NotConfigured:
                logger.LogWarning("Mail transport is not configured; delivery was skipped; kind={Kind}; transport={Transport}.", kind, transport);
                return MailResult.NotConfigured;
            default:
                logger.LogError("Mail delivery failed; kind={Kind}; transport={Transport}; failure={Failure}; {Diagnostics}.", kind, transport, outcome.Failure, outcome.Diagnostics);
                return MailResult.Failed(outcome.Failure);
        }
    }
}
