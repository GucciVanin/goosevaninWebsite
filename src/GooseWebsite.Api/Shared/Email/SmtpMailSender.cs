using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace GooseWebsite.Api.Shared.Email;

public sealed class SmtpMailSender(IOptions<SmtpOptions> options, ILogger<SmtpMailSender> logger) : ISmtpMailSender
{
    private readonly SmtpOptions _options = options.Value;

    public MailAddress? FromAddress => _options.IsConfigured
        ? new MailAddress(_options.FromEmail, _options.FromName)
        : null;

    public async Task<bool> SendAsync(params MailMessage[] messages)
    {
        if (!_options.IsConfigured)
        {
            logger.LogWarning("SMTP configuration is incomplete; email delivery was skipped.");
            return false;
        }

        using var smtp = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30_000
        };

        try
        {
            foreach (var message in messages)
            {
                await smtp.SendMailAsync(message);
            }

            logger.LogInformation("SMTP delivery succeeded; messageCount={MessageCount}.", messages.Length);
            return true;
        }
        catch (Exception exception)
        {
            // Log categories only (exception types and the SMTP status code): provider response
            // text can contain recipient addresses.
            logger.LogError(
                "SMTP delivery failed; exceptionType={ExceptionType}; smtpStatusCode={SmtpStatusCode}; innerExceptionType={InnerExceptionType}.",
                exception.GetType().Name,
                (exception as SmtpException)?.StatusCode,
                exception.InnerException?.GetType().Name);
            return false;
        }
    }
}
