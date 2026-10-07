namespace GooseWebsite.Api.Shared.Email;

/// <summary>
/// MailerSend HTTP API settings bound from the <c>MailerSend</c> configuration section. The API key is
/// a secret: supply it through user-secrets or environment variables, never source control.
/// </summary>
public sealed class MailerSendOptions
{
    public const string SectionName = "MailerSend";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.mailersend.com/v1/";

    /// <summary>Sender address. On a MailerSend trial domain this must be an address on that domain.</summary>
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Gustavo Couto Vanin";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(FromEmail);
}
