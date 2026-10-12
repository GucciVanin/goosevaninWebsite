using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;

namespace GooseWebsite.Api.Modules.Contact.Services;

/// <summary>
/// The Contact module's interface. A Contact submission is validated, rate limited and held as a
/// Pending message while a Verification link is emailed to the sender; following the link delivers it.
/// Callers learn only the outcome; the order of checks, the retry rules and the logging policy live behind it.
/// </summary>
public interface IContactSubmissionService
{
    /// <summary>Checks the submission and, if allowed, emails the sender a Verification link.</summary>
    Task<ContactSubmitOutcome> SubmitAsync(ContactMessage message, string clientKey);

    /// <summary>
    /// Completes Verification and delivers the message. A failed delivery keeps the Pending message so the
    /// same link can be retried until it expires, and remembers whether Gustavo was already notified.
    /// </summary>
    Task<ContactVerifyOutcome> VerifyAsync(string token);
}
