using Microsoft.AspNetCore.Authorization;

namespace GooseWebsite.Api.Modules.Accounts.Authorization;

// Marks operations that require a currently confirmed Reader account.
public sealed class VerifiedReaderRequirement : IAuthorizationRequirement;