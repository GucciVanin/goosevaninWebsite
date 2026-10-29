using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Hosting;

namespace GooseWebsite.Api.Modules.Accounts.Services;

// Renders the account verification email (HTML plus plain text) and hands it to the shared mail service.
public sealed class AccountEmailSender(IMailService mail, PublicLinkBuilder links) : IAccountEmailSender
{
    public async Task<bool> SendVerificationEmailAsync(string email, string displayName, string verificationUrl)
    {
        var plain = AccountVerificationEmailRenderer.RenderPlainText(displayName, verificationUrl);
        var html = AccountVerificationEmailRenderer.RenderHtml(
            displayName,
            verificationUrl,
            links.SiteLink(AccountVerificationEmailRenderer.LogoPath),
            links.SiteLink(string.Empty));

        var result = await mail.SendAsync(new RenderedMail(
            new MailAddressee(email, displayName),
            AccountVerificationEmailRenderer.Subject,
            html,
            plain));

        return result.Succeeded;
    }
}
