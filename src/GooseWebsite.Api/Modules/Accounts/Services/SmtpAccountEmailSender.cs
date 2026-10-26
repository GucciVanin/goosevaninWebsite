using System.Net.Mail;
using System.Net.Mime;
using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Hosting;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Delivers account verification links through the shared SMTP transport, as HTML plus plain text.
public sealed class SmtpAccountEmailSender(ISmtpMailSender mailSender, PublicLinkBuilder links) : IAccountEmailSender
{
    public async Task<bool> SendVerificationEmailAsync(string email, string displayName, string verificationUrl)
    {
        var from = mailSender.FromAddress;
        if (from is null)
        {
            // Delegating still logs the "SMTP not configured" warning in one place.
            return await mailSender.SendAsync();
        }

        var plain = AccountVerificationEmailRenderer.RenderPlainText(displayName, verificationUrl);
        var html = AccountVerificationEmailRenderer.RenderHtml(
            displayName,
            verificationUrl,
            links.SiteLink(AccountVerificationEmailRenderer.LogoPath),
            links.SiteLink(string.Empty));

        using var message = new MailMessage
        {
            From = from,
            Subject = AccountVerificationEmailRenderer.Subject
        };
        message.To.Add(new MailAddress(email, displayName));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plain, null, MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));

        return await mailSender.SendAsync(message);
    }
}
