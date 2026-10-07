namespace GooseWebsite.Api.Modules.Contact.Models;

/// <summary>
/// Variable names the MailerSend contact templates read. They are case-sensitive and must match the
/// <c>{{...}}</c> placeholders in the templates (see <c>Docs/email-templates/README.md</c>). A variable
/// the request omits renders as its own name.
/// </summary>
public static class ContactEmailTemplateVariables
{
    public const string Name = "name";
    public const string SenderEmail = "sender_email";
    public const string Reason = "reason";
    public const string Message = "message";
    public const string VerificationUrl = "verification_url";
    public const string SiteUrl = "site_url";
    public const string LogoUrl = "logo_url";
}
