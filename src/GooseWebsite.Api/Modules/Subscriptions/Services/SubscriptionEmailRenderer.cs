using System.Net;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Renders the subscription confirmation email as branded HTML plus a plain-text alternative, in the same
// brand layout as the account email.
// The HTML uses tables and inline styles only: no web fonts, scripts, remote CSS or tracking images.
// The only remote resource is the logo, served by the site itself. Keep the wording in step with
// Docs/email-templates/README.md.
internal static class SubscriptionEmailRenderer
{
    public const string ConfirmationSubject = "Confirm your subscription";
    public const string LogoPath = "/email/gcv-logo.png";

    private const string ExpirySentence = "This link expires in 7 days.";
    private const string IgnoreSentence = "If you did not ask to subscribe, you can ignore this message and nothing will be sent to you.";
    private const string Mono = "Consolas,'SFMono-Regular','Courier New',monospace";
    private const string Sans = "'Segoe UI','Helvetica Neue',Arial,sans-serif";

    private const string UnsubscribeSentence = "Changed your mind? You can unsubscribe at any time:";

    public static string RenderConfirmationPlainText(string displayName, string verificationUrl, string unsubscribeUrl) =>
        $"Hi {displayName},\n\nConfirm your email address to receive an email when Gustavo publishes a new story or project:\n\n{verificationUrl}\n\n{ExpirySentence} {IgnoreSentence}\n\n{UnsubscribeSentence}\n{unsubscribeUrl}\n\nGustavo Couto Vanin";

    public static string RenderConfirmationHtml(string displayName, string verificationUrl, string unsubscribeUrl, string logoUrl, string siteUrl)
    {
        var name = WebUtility.HtmlEncode(displayName);
        var link = WebUtility.HtmlEncode(verificationUrl);
        var unsubscribe = WebUtility.HtmlEncode(unsubscribeUrl);
        var logo = WebUtility.HtmlEncode(logoUrl);
        var site = WebUtility.HtmlEncode(siteUrl);

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="color-scheme" content="dark">
<meta name="supported-color-schemes" content="dark">
<title>{{ConfirmationSubject}}</title>
</head>
<body style="margin:0; padding:0; background-color:#05060f;" bgcolor="#05060f">
<div style="display:none; max-height:0; overflow:hidden; mso-hide:all; font-size:1px; line-height:1px; color:#05060f;">Confirm your email address to get updates from Gustavo. The link expires in 7 days.</div>
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" bgcolor="#05060f" style="background-color:#05060f;">
<tr><td align="center" style="padding:28px 12px;">
  <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:100%; max-width:600px;">
    <tr><td style="font-size:0; line-height:0;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr>
        <td width="50%" height="3" bgcolor="#ff2bd6" style="height:3px; line-height:3px; font-size:0;">&nbsp;</td>
        <td width="50%" height="3" bgcolor="#19e6ff" style="height:3px; line-height:3px; font-size:0;">&nbsp;</td>
      </tr></table>
    </td></tr>
    <tr><td align="center" bgcolor="#000000" style="background-color:#000000; padding:26px 32px 18px 32px; border-left:1px solid #23264a; border-right:1px solid #23264a;">
      <a href="{{site}}" style="text-decoration:none;"><img src="{{logo}}" width="150" height="125" alt="GCV, Gustavo Couto Vanin" style="display:block; width:150px; height:auto; margin:0 auto; border:0;"></a>
      <div style="padding-top:14px; font-family:{{Mono}}; font-size:11px; letter-spacing:4px; text-transform:uppercase; color:#8b90b8;">Build <span style="color:#ff2bd6;">&middot;</span> Break <span style="color:#19e6ff;">&middot;</span> Improve</div>
    </td></tr>
    <tr><td bgcolor="#0b0d1f" style="background-color:#0b0d1f; padding:34px 40px 36px 40px; border-left:1px solid #23264a; border-right:1px solid #23264a; border-top:1px solid #23264a;">
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
    </td></tr>
    <tr><td align="center" bgcolor="#000000" style="background-color:#000000; padding:22px 32px 26px 32px; border:1px solid #23264a;">
      <div style="font-family:{{Mono}}; font-size:11px; letter-spacing:3px; text-transform:uppercase; color:#8b90b8; line-height:20px;">Curious mind. Practical solutions. Real impact.</div>
      <div style="padding-top:10px; font-family:{{Sans}}; font-size:12px; line-height:18px; color:#6c7199;">Gustavo Couto Vanin &middot; <a href="{{site}}" style="color:#19e6ff; text-decoration:none;">{{site}}</a></div>
      <div style="padding-top:10px; font-family:{{Sans}}; font-size:12px; line-height:18px; color:#6c7199;">{{UnsubscribeSentence}} <a href="{{unsubscribe}}" style="color:#19e6ff; text-decoration:underline;">Unsubscribe</a></div>
    </td></tr>
    <tr><td style="font-size:0; line-height:0;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr>
        <td width="50%" height="3" bgcolor="#19e6ff" style="height:3px; line-height:3px; font-size:0;">&nbsp;</td>
        <td width="50%" height="3" bgcolor="#ff2bd6" style="height:3px; line-height:3px; font-size:0;">&nbsp;</td>
      </tr></table>
    </td></tr>
  </table>
</td></tr>
</table>
</body>
</html>
""";
    }
}
