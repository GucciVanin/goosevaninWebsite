namespace GooseWebsite.Api.Tests;

// Models the only CSRF response data needed by HTTP integration tests.
internal sealed record CsrfResponse(string RequestToken);