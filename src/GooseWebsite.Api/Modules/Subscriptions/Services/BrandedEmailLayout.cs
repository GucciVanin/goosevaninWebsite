using System.Net;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// The brand frame shared by every subscription email: gradient rule, logo, the card for the message, and the footer
// with the unsubscribe line. Tables and inline styles only (no web fonts, scripts, remote CSS or tracking images);
// the only remote resource is the logo, served by the site itself. The caller passes HTML it has already encoded.
internal static class BrandedEmailLayout
{
    public const string Mono = "Consolas,'SFMono-Regular','Courier New',monospace";
    public const string Sans = "'Segoe UI','Helvetica Neue',Arial,sans-serif";
    public const string UnsubscribeSentence = "Changed your mind? You can unsubscribe at any time:";

    /// <param name="title">Document title (plain text; encoded here).</param>
    /// <param name="preheader">The inbox preview line (plain text; encoded here).</param>
    /// <param name="cardHtml">The HTML inside the message card, already encoded.</param>
    /// <param name="unsubscribeUrl">The personal unsubscribe link (plain; encoded here).</param>
    public static string Render(string title, string preheader, string cardHtml, string unsubscribeUrl, string logoUrl, string siteUrl)
    {
        var unsubscribe = WebUtility.HtmlEncode(unsubscribeUrl);
        var logo = WebUtility.HtmlEncode(logoUrl);
        var site = WebUtility.HtmlEncode(siteUrl);
        var documentTitle = WebUtility.HtmlEncode(title);
        var inboxPreview = WebUtility.HtmlEncode(preheader);

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="color-scheme" content="dark">
<meta name="supported-color-schemes" content="dark">
<title>{{documentTitle}}</title>
</head>
<body style="margin:0; padding:0; background-color:#05060f;" bgcolor="#05060f">
<div style="display:none; max-height:0; overflow:hidden; mso-hide:all; font-size:1px; line-height:1px; color:#05060f;">{{inboxPreview}}</div>
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
{{cardHtml}}
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
