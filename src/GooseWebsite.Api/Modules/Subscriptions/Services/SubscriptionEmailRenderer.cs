using System.Net;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Renders the subscription confirmation email as branded HTML plus a plain-text alternative, in the same
// brand layout as the account email (the frame is BrandedEmailLayout, shared with the announcement email).
// The HTML uses tables and inline styles only: no web fonts, scripts, remote CSS or tracking images.
// The only remote resource is the logo, served by the site itself. Keep the wording in step with
// Docs/email-templates/README.md.
internal static class SubscriptionEmailRenderer
{
    public const string ConfirmationSubject = "Confirm your subscription";
    public const string LogoPath = "/email/gcv-logo.png";

    private const string ExpirySentence = "This link expires in 7 days.";
    private const string IgnoreSentence = "If you did not ask to subscribe, you can ignore this message and nothing will be sent to you.";
    private const string Mono = BrandedEmailLayout.Mono;
    private const string Sans = BrandedEmailLayout.Sans;
    private const string UnsubscribeSentence = BrandedEmailLayout.UnsubscribeSentence;

    public static string RenderConfirmationPlainText(string displayName, string verificationUrl, string unsubscribeUrl) =>
        $"Hi {displayName},\n\nConfirm your email address to receive an email when Gustavo publishes a new story or project:\n\n{verificationUrl}\n\n{ExpirySentence} {IgnoreSentence}\n\n{UnsubscribeSentence}\n{unsubscribeUrl}\n\nGustavo Couto Vanin";

    public static string RenderConfirmationHtml(string displayName, string verificationUrl, string unsubscribeUrl, string logoUrl, string siteUrl)
    {
        var name = WebUtility.HtmlEncode(displayName);
        var link = WebUtility.HtmlEncode(verificationUrl);

        var card = $$"""
      <div style="font-family:{{Mono}}; font-size:12px; letter-spacing:3px; text-transform:uppercase; color:#ff2bd6; padding-bottom:12px;">// Subscription</div>
      <h1 style="font-family:{{Sans}}; font-weight:800; font-size:30px; line-height:36px; letter-spacing:1px; text-transform:uppercase; color:#ffffff; margin:0 0 16px 0;">Confirm your<br><span style="color:#19e6ff;">subscription</span></h1>
      <p style="font-family:{{Sans}}; font-size:15px; line-height:24px; color:#c9cdf0; margin:0 0 16px 0;">Hi {{name}},</p>
      <p style="font-family:{{Sans}}; font-size:15px; line-height:24px; color:#c9cdf0; margin:0 0 16px 0;">Confirm your email address to receive an email when Gustavo publishes a new story or project.</p>
      <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:6px 0 4px 0;"><tr>
        <td bgcolor="#0b0d1f" style="border:1px solid #19e6ff; border-left:3px solid #ff2bd6; border-bottom:3px solid #ff2bd6; background-color:#0b0d1f;">
          <a href="{{link}}" target="_blank" style="display:block; padding:15px 30px; font-family:{{Mono}}; font-size:13px; font-weight:bold; letter-spacing:3px; text-transform:uppercase; color:#ffffff; text-decoration:none;">Confirm subscription &rarr;</a>
        </td>
      </tr></table>
      <p style="font-family:{{Sans}}; font-size:13px; line-height:20px; color:#8b90b8; margin:22px 0 6px 0;">{{ExpirySentence}} If the button does not work, copy this address into your browser:</p>
      <p style="font-family:{{Mono}}; font-size:12px; line-height:18px; color:#19e6ff; word-break:break-all; margin:0 0 20px 0;">{{link}}</p>
      <p style="font-family:{{Sans}}; font-size:13px; line-height:20px; color:#8b90b8; margin:0;">{{IgnoreSentence}}</p>
""";

        return BrandedEmailLayout.Render(
            ConfirmationSubject,
            "Confirm your email address to get updates from Gustavo. The link expires in 7 days.",
            card,
            unsubscribeUrl,
            logoUrl,
            siteUrl);
    }
}
