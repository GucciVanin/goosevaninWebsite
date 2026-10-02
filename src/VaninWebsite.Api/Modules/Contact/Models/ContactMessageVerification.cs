namespace VaninWebsite.Api.Modules.Contact.Models;

// Preserves the submitted contact details until the sender verifies the message.
public sealed record ContactMessageVerification(string Name, string Email, string Reason, string Message, DateTime ExpiresAtUtc);