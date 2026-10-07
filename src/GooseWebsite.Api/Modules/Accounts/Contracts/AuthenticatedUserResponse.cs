namespace GooseWebsite.Api.Modules.Accounts.Contracts;

// Limits authentication responses to the profile and authorization state the client needs.
public sealed record AuthenticatedUserResponse(string DisplayName, bool IsAdmin);