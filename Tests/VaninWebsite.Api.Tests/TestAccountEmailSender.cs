using System.Collections.Concurrent;
using VaninWebsite.Api.Modules.Accounts.Services;

namespace VaninWebsite.Api.Tests;

// Captures verification messages for HTTP tests without contacting an SMTP provider.
internal sealed class TestAccountEmailSender : IAccountEmailSender
{
    private readonly ConcurrentQueue<(string Email, string DisplayName, string Url)> _sentMessages = new();

    public IReadOnlyCollection<(string Email, string DisplayName, string Url)> SentMessages => _sentMessages.ToArray();

    public bool ShouldFail { get; set; }

    public Task<bool> SendVerificationEmailAsync(string email, string displayName, string verificationUrl)
    {
        if (ShouldFail)
        {
            return Task.FromResult(false);
        }

        _sentMessages.Enqueue((email, displayName, verificationUrl));
        return Task.FromResult(true);
    }
}