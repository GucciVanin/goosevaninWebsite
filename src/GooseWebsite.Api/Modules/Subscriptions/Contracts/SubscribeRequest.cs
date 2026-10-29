namespace GooseWebsite.Api.Modules.Subscriptions.Contracts;

// Every property may be missing or wrong: the service decides, and the answer never depends on it. "Website" is the
// honeypot field; people never see it, so any text in it means a script filled the form.
public sealed record SubscribeRequest(string? Name, string? Email, string? Website);
