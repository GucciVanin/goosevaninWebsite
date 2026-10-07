namespace GooseWebsite.Api.Modules.Contact.Models;

// Carries the generated token and the URL needed by the confirmation message.
public sealed record ContactVerificationToken(string Token, string VerificationUrl);