using System.Net.Mail;
using VaninWebsite.Api.Shared.Email;

namespace VaninWebsite.Api.Modules.Accounts.Services;

// Delivers account verification links through the shared SMTP transport.
public sealed class SmtpAccountEmailSender(ISmtpMailSender mailSender) : IAccountEmailSender
{
    public async Task<bool> SendVerificationEmailAsync(string email, string displayName, string verificationUrl)
    {
        var from = mailSender.FromAddress;
        if (from is null)
        {
            // Delegating still logs the "SMTP not configured" warning in one place.
            return await mailSender.SendAsync();
        }

        using var message = new MailMessage
        {
            From = from,
            Subject = "Confirm your email address",
            Body = $"Hi {displayName},\n\nConfirm your email address to finish creating your Reader account:\n\n{verificationUrl}\n\nThis link expires in 24 hours. If you did not request this account, you can ignore this message.",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(email, displayName));

        return await mailSender.SendAsync(message);
    }
}
