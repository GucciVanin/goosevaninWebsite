using System.Net.Mail;

namespace VaninWebsite.Api.Shared.Email;

/// <summary>
/// The only place the application talks to the SMTP provider. Modules build their own messages
/// and hand them here, so swapping the provider touches one class.
/// </summary>
public interface ISmtpMailSender
{
    /// <summary>The configured sender address and display name, or null when SMTP is not configured.</summary>
    MailAddress? FromAddress { get; }

    /// <summary>
    /// Sends the messages in order over one connection. Returns false (and logs only the failure
    /// category) when SMTP is not configured or any send fails.
    /// </summary>
    Task<bool> SendAsync(params MailMessage[] messages);
}
