namespace GooseWebsite.Api.Modules.Accounts.Services;

// Carries the reset outcome without coupling callers to identity error details.
public sealed record AdminPasswordResetResult(bool Succeeded, string ErrorMessage)
{
    public static AdminPasswordResetResult Success() => new(true, string.Empty);
    public static AdminPasswordResetResult Failure(string message) => new(false, message);
}
