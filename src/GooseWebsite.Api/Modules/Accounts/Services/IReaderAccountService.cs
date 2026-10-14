using GooseWebsite.Api.Modules.Accounts.Contracts;

namespace GooseWebsite.Api.Modules.Accounts.Services;

/// <summary>What the caller may learn about a registration; every real outcome is <see cref="Accepted"/>.</summary>
public enum ReaderRegistrationOutcome
{
    /// <summary>Reader registration is switched off; the endpoint behaves as if it did not exist.</summary>
    Disabled,

    /// <summary>The input was not usable. Answered like success, so nothing about accounts can be probed.</summary>
    InvalidInput,

    /// <summary>
    /// The request was handled. Whether an account was created, already existed, or could not be created
    /// is deliberately not revealed.
    /// </summary>
    Accepted
}

/// <summary>
/// The Accounts module's interface for Reader accounts: registration, another Verification link, and
/// Verification itself. Nothing here reveals whether an email address already has an account.
/// </summary>
public interface IReaderAccountService
{
    /// <summary>True when Reader registration is switched on (<c>Auth:ReaderRegistration:Enabled</c>).</summary>
    bool RegistrationEnabled { get; }

    /// <summary>
    /// Creates a Reader and emails a Verification link. A role assignment that fails removes the account again,
    /// so no half-created account remains. <paramref name="inputIsValid"/> is the caller's model validation result.
    /// </summary>
    Task<ReaderRegistrationOutcome> RegisterAsync(RegisterRequest request, bool inputIsValid);

    /// <summary>Emails a new Verification link when the address belongs to an unverified Reader; otherwise does nothing.</summary>
    Task ResendVerificationAsync(string email);

    /// <summary>Verifies the address and returns true; returns false for an unknown user, an already verified one, or a bad token.</summary>
    Task<bool> VerifyEmailAsync(string userId, string token);
}
