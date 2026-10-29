using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Hosting;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Renders the subscription emails (HTML plus plain text) and hands them to the shared mail service.
public sealed class SubscriptionEmailSender(IMailService mail, PublicLinkBuilder links, ISubscriptionLinks subscriptionLinks) : ISubscriptionEmailSender
{
    public async Task<bool> SendConfirmationAsync(int subscriberId, string email, string name, string confirmationUrl)
    {
        // Every subscription mail carries the unsubscribe link in the body and in the List-Unsubscribe headers.
        var unsubscribeUrl = subscriptionLinks.UnsubscribePageUrl(subscriberId);
        var result = await mail.SendAsync(new RenderedMail(
            new MailAddressee(email, name),
            SubscriptionEmailRenderer.ConfirmationSubject,
            SubscriptionEmailRenderer.RenderConfirmationHtml(
                name,
                confirmationUrl,
                unsubscribeUrl,
                links.SiteLink(SubscriptionEmailRenderer.LogoPath),
                links.SiteLink(string.Empty)),
            SubscriptionEmailRenderer.RenderConfirmationPlainText(name, confirmationUrl, unsubscribeUrl),
            subscriptionLinks.UnsubscribeHeaders(subscriberId)));

        return result.Succeeded;
    }
}
