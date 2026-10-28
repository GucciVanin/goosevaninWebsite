using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Options;

namespace GooseWebsite.Api.Shared.Email;

/// <summary>
/// The only place the application talks to the SMTP provider. It reports categories back to
/// <see cref="MailService"/>; it never logs.
/// </summary>
public sealed class SmtpMailTransport(IOptions<SmtpOptions> options) : IRenderedMailTransport
{
    private readonly SmtpOptions _options = options.Value;

    public string Name => "SMTP";

    public async Task<MailTransportOutcome> SendAsync(RenderedMail mail, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return MailTransportOutcome.NotConfigured;
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
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = mail.Subject
            };
            message.To.Add(new MailAddress(mail.To.Email, mail.To.Name));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(mail.PlainText, null, MediaTypeNames.Text.Plain));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(mail.Html, null, MediaTypeNames.Text.Html));

            await smtp.SendMailAsync(message, cancellationToken);
            return MailTransportOutcome.Accepted;
        }
        catch (Exception exception)
        {
            // Categories only (exception types and the SMTP status code): provider response text can
            // contain recipient addresses, and a malformed-address exception message is the address.
            var diagnostics = $"exceptionType={exception.GetType().Name}; smtpStatusCode={(exception as SmtpException)?.StatusCode}; innerExceptionType={exception.InnerException?.GetType().Name}";
            return new MailTransportOutcome(MailFailure.TransportError, diagnostics);
        }
    }
}
