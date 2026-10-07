namespace GooseWebsite.Api.Modules.Contact.Models;

// Preserves the submitted contact details until the sender verifies the message. OwnerNotified is
// set when a delivery attempt reached Gustavo but failed afterwards, so a retry does not notify him twice.
public sealed record ContactMessageVerification(string Name, string Email, string Reason, string Message, DateTime ExpiresAtUtc, bool OwnerNotified = false);
