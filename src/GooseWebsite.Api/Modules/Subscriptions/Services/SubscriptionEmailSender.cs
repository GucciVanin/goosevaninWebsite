using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Hosting;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Renders the subscription emails (HTML plus plain text) and hands them to the shared mail service.
public sealed class SubscriptionEmailSender(IMailService mail, PublicLinkBuilder links) : ISubscriptionEmailSender
{
    public async Task<bool> SendConfirmationAsync(string email, string name, string confirmationUrl)
    {
        var result = await mail.SendAsync(new RenderedMail(
            new MailAddressee(email, name),
            SubscriptionEmailRenderer.ConfirmationSubject,
            SubscriptionEmailRenderer.RenderConfirmationHtml(
                name,
                confirmationUrl,
                links.SiteLink(SubscriptionEmailRenderer.LogoPath),
                links.SiteLink(string.Empty)),
            SubscriptionEmailRenderer.RenderConfirmationPlainText(name, confirmationUrl)));

        return result.Succeeded;
    }
}
