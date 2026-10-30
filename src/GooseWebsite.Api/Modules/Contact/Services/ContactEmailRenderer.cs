using System.Globalization;
using System.Net;
using GooseWebsite.Api.Shared.Email;

namespace GooseWebsite.Api.Modules.Contact.Services;

// Renders the three contact emails as branded HTML plus a plain-text alternative (TE11). The designs are
// Docs/email-templates/*.html; the frame is the shared BrandedEmailLayout. Every visitor value is HTML-encoded
// in the HTML part and the message keeps its line breaks (as <br>). The plain-text part carries the values as
// typed. A name goes into subjects, greetings and display names only after control, line-separator and format
// characters are removed, so it can never add a header or a recipient.
public static class ContactEmailRenderer
{
    public const string ConfirmationSubject = "Confirm your email to send your message to Gustavo";
    public const string ReceiptSubject = "Your message has been received";

    private const string Mono = BrandedEmailLayout.Mono;
    private const string Sans = BrandedEmailLayout.Sans;
    private const string FallbackName = "there";
    private const string ConfirmationFooter = "You are receiving this because this address was entered in the contact form on this site.";
    private const string ReceiptFooter = "This confirms that your message was delivered. No copy is kept except in this email.";
    private const string OwnerFooter = "Sent by the contact form on this site after the sender confirmed their email address. Replying to this email replies to the sender.";

    /// <summary>The name as it may appear in a subject, a greeting or a display name: one line, no control characters.</summary>
    public static string CleanName(string name)
    {
        var kept = name.Where(character =>
        {
            if (char.IsControl(character))
            {
                return false;
            }

            var category = char.GetUnicodeCategory(character);
            return category is not (UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.Format);
        }).ToArray();

        return new string(kept).Trim();
    }

    public static string OwnerNoticeSubject(string name)
    {
        var clean = CleanName(name);
        return $"New contact message from {(clean.Length == 0 ? "a visitor" : clean)}";
    }

    public static RenderedMail Confirmation(MailAddressee to, string verificationUrl, string logoUrl, string siteUrl)
    {
        var name = Greeting(to.Name);
        var plain = $"Hi {name},\n\nThanks for reaching out to Gustavo Couto Vanin. Your message has not been sent yet. It is delivered only after you confirm that this address is yours:\n\n{verificationUrl}\n\nThe link works once and expires in 30 minutes. Did not write to Gustavo? Ignore this email and nothing will be sent.\n\nGustavo Couto Vanin";

        var link = Encode(verificationUrl);
        var card = $$"""
      <div style="font-family:{{Mono}}; font-size:12px; letter-spacing:3px; text-transform:uppercase; color:#ff2bd6; padding-bottom:12px;">// Verification required</div>
      <h1 style="font-family:{{Sans}}; font-weight:800; font-size:30px; line-height:36px; letter-spacing:1px; text-transform:uppercase; color:#ffffff; margin:0 0 16px 0;">Confirm your<br><span style="color:#19e6ff;">email address</span></h1>
      <p style="font-family:{{Sans}}; font-size:15px; line-height:24px; color:#c9cdf0; margin:0 0 16px 0;">Hi {{Encode(name)}},</p>
      <p style="font-family:{{Sans}}; font-size:15px; line-height:24px; color:#c9cdf0; margin:0 0 16px 0;">Thanks for reaching out to Gustavo Couto Vanin. Your message has <strong style="color:#ffffff;">not been sent yet</strong>. It is delivered only after you confirm that this address is yours.</p>
{{Button(link, "Confirm and send")}}
      <p style="font-family:{{Sans}}; font-size:13px; line-height:20px; color:#8b90b8; margin:22px 0 6px 0;">The link works once and expires in <strong style="color:#c9cdf0;">30 minutes</strong>. If the button does not work, copy this address into your browser:</p>
      <p style="font-family:{{Mono}}; font-size:12px; line-height:18px; color:#19e6ff; word-break:break-all; margin:0 0 20px 0;">{{link}}</p>
      <p style="font-family:{{Sans}}; font-size:13px; line-height:20px; color:#8b90b8; margin:0;">Did not write to Gustavo? Ignore this email and nothing will be sent.</p>
""";

        return new RenderedMail(
            to with { Name = CleanName(to.Name) },
            ConfirmationSubject,
            BrandedEmailLayout.Render(ConfirmationSubject, "Confirm your email address to send your message to Gustavo. The link expires in 30 minutes.", card, null, logoUrl, siteUrl, ConfirmationFooter),
            plain);
    }

    public static RenderedMail OwnerNotice(MailAddressee owner, string senderName, string senderEmail, string reason, string message, string logoUrl, string siteUrl)
    {
        var name = Greeting(senderName);
        var subject = OwnerNoticeSubject(senderName);
        var plain = $"New contact message\n\nFrom: {name}\nEmail: {senderEmail} (verified)\nReason: {reason}\n\nMessage:\n{message}\n\nReplying to this email replies to the sender.\n\nGustavo Couto Vanin";

        var emailCell = $"<a href=\"mailto:{Encode(Uri.EscapeDataString(senderEmail))}\" style=\"color:#19e6ff; text-decoration:none;\">{Encode(senderEmail)}</a> <span style=\"color:#19e6ff; font-size:12px;\">&#10003; verified</span>";
        var card = $$"""
      <div style="font-family:{{Mono}}; font-size:12px; letter-spacing:3px; text-transform:uppercase; color:#ff2bd6; padding-bottom:12px;">// Incoming transmission</div>
      <h1 style="font-family:{{Sans}}; font-weight:800; font-size:30px; line-height:36px; letter-spacing:1px; text-transform:uppercase; color:#ffffff; margin:0 0 16px 0;">New contact<br><span style="color:#ff2bd6;">message</span></h1>
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:6px 0 22px 0; border-top:1px solid #23264a;">
{{Row("From", Encode(name))}}
{{Row("Email", emailCell)}}
{{Row("Reason", Encode(reason))}}
      </table>
      <div style="font-family:{{Mono}}; font-size:11px; letter-spacing:3px; text-transform:uppercase; color:#8b90b8; padding:0;">Message</div>
{{MessageBox("#ff2bd6", null, message)}}
{{Button($"mailto:{Encode(Uri.EscapeDataString(senderEmail))}", $"Reply to {Truncate(name)}")}}
""";

        return new RenderedMail(
            owner,
            subject,
            BrandedEmailLayout.Render("New contact message", $"New message from {name}: {reason}", card, null, logoUrl, siteUrl, OwnerFooter),
            plain,
            ReplyTo: new MailAddressee(senderEmail, CleanName(senderName)));
    }

    public static RenderedMail Receipt(MailAddressee sender, MailAddressee owner, string reason, string message, string logoUrl, string siteUrl)
    {
        var name = Greeting(sender.Name);
        var plain = $"Hi {name},\n\nThank you for contacting Gustavo Couto Vanin. Your email address is confirmed and your message has been delivered. He will get back to you as soon as possible.\n\nYour message ({reason}):\n{message}\n\nYou can reply directly to this email to continue the conversation.\n\nGustavo Couto Vanin";

        var card = $$"""
      <div style="font-family:{{Mono}}; font-size:12px; letter-spacing:3px; text-transform:uppercase; color:#ff2bd6; padding-bottom:12px;">// Transmission received</div>
      <h1 style="font-family:{{Sans}}; font-weight:800; font-size:30px; line-height:36px; letter-spacing:1px; text-transform:uppercase; color:#ffffff; margin:0 0 16px 0;">Message<br><span style="color:#19e6ff;">received</span></h1>
      <p style="font-family:{{Sans}}; font-size:15px; line-height:24px; color:#c9cdf0; margin:0 0 16px 0;">Hi {{Encode(name)}},</p>
      <p style="font-family:{{Sans}}; font-size:15px; line-height:24px; color:#c9cdf0; margin:0 0 16px 0;">Thank you for contacting Gustavo Couto Vanin. Your email address is confirmed and your message has been delivered. He will get back to you as soon as possible.</p>
      <div style="font-family:{{Mono}}; font-size:11px; letter-spacing:3px; text-transform:uppercase; color:#8b90b8; padding:6px 0 0 0;">Your message</div>
{{MessageBox("#19e6ff", reason, message)}}
      <p style="font-family:{{Sans}}; font-size:13px; line-height:20px; color:#8b90b8; margin:0 0 18px 0;">You can reply directly to this email to continue the conversation.</p>
{{Button(Encode(siteUrl), "Visit the site")}}
""";

        return new RenderedMail(
            sender with { Name = CleanName(sender.Name) },
            ReceiptSubject,
            BrandedEmailLayout.Render("Message received", "Your message to Gustavo has been delivered.", card, null, logoUrl, siteUrl, ReceiptFooter),
            plain,
            ReplyTo: owner);
    }

    private static string Greeting(string name)
    {
        var clean = CleanName(name);
        return clean.Length == 0 ? FallbackName : clean;
    }

    private static string Truncate(string name)
    {
        if (name.Length <= 60)
        {
            return name;
        }

        // Never cut between the two halves of a surrogate pair.
        var length = char.IsHighSurrogate(name[59]) ? 59 : 60;
        return name[..length];
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    // Encodes the text, then turns each line break into <br> so the message reads as written in every mail client.
    private static string EncodeWithBreaks(string value) =>
        Encode(value).Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "<br>");

    private static string Row(string label, string valueHtml) => $$"""
        <tr>
          <td width="110" valign="top" style="font-family:{{Mono}}; font-size:11px; letter-spacing:3px; text-transform:uppercase; color:#8b90b8; padding:12px 0; border-bottom:1px solid #23264a;">{{label}}</td>
          <td valign="top" style="font-family:{{Sans}}; font-size:15px; color:#ffffff; padding:12px 0; border-bottom:1px solid #23264a;">{{valueHtml}}</td>
        </tr>
""";

    private static string MessageBox(string accent, string? reason, string message)
    {
        var reasonLine = reason is null
            ? string.Empty
            : $"          <div style=\"font-family:{Mono}; font-size:11px; letter-spacing:3px; text-transform:uppercase; color:#8b90b8; padding-bottom:8px;\">{Encode(reason)}</div>" + Environment.NewLine;

        return $$"""
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:8px 0 18px 0;"><tr>
        <td width="3" bgcolor="{{accent}}" style="width:3px; background-color:{{accent}}; font-size:0;">&nbsp;</td>
        <td bgcolor="#080a18" style="background-color:#080a18; padding:16px 18px; border:1px solid #23264a; border-left:0;">
{{reasonLine}}          <div style="font-family:{{Sans}}; font-size:14px; line-height:22px; color:#e8eaff;">{{EncodeWithBreaks(message)}}</div>
        </td>
      </tr></table>
""";
    }

    // hrefHtml is already HTML-encoded by the caller; labelHtml is encoded here.
    private static string Button(string hrefHtml, string label) => $$"""
      <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:6px 0 4px 0;"><tr>
        <td bgcolor="#0b0d1f" style="border:1px solid #19e6ff; border-left:3px solid #ff2bd6; border-bottom:3px solid #ff2bd6; background-color:#0b0d1f;">
          <a href="{{hrefHtml}}" target="_blank" style="display:block; padding:15px 30px; font-family:{{Mono}}; font-size:13px; font-weight:bold; letter-spacing:3px; text-transform:uppercase; color:#ffffff; text-decoration:none;">{{Encode(label)}} &rarr;</a>
        </td>
      </tr></table>
""";
}
