namespace VaninWebsite.Api.Modules.Accounts.Services;

// Carries the provisioning outcome without coupling callers to identity error details.
public sealed record AdminProvisioningResult(bool Succeeded, string ErrorMessage)
{
    public static AdminProvisioningResult Success() => new(true, string.Empty);
    public static AdminProvisioningResult Failure(string message) => new(false, message);
}