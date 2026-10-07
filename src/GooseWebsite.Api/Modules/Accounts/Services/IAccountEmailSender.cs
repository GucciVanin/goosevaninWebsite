namespace GooseWebsite.Api.Modules.Accounts.Services;

// Keeps account verification mail independent from public contact-message delivery.
public interface IAccountEmailSender
{
    Task<bool> SendVerificationEmailAsync(string email, string displayName, string verificationUrl);
}