using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Modules.Contact.Services;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Protects contact submission limits and verification behavior from regressions.
public sealed class ContactFeatureTests
{
    [Fact]
    public void ContactSubmissionAbuseGuard_RejectsAfterConfiguredThreshold()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Contact:RateLimit:MaxRequestsPerWindow"] = "2",
                ["Contact:RateLimit:WindowMinutes"] = "10"
            })
            .Build();

        var guard = new ContactSubmissionAbuseGuard(configuration);

        Assert.True(guard.TryAllow("client-1", out _));
        Assert.True(guard.TryAllow("client-1", out _));
        Assert.False(guard.TryAllow("client-1", out var rejectionReason));
        Assert.Equal("rate_limit", rejectionReason);
    }

    [Fact]
    public void ContactSubmissionAbuseGuard_EnforcesThresholdForConcurrentRequests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Contact:RateLimit:MaxRequestsPerWindow"] = "3",
                ["Contact:RateLimit:WindowMinutes"] = "10"
            })
            .Build();
        var guard = new ContactSubmissionAbuseGuard(configuration);
        var allowed = 0;

        Parallel.For(0, 100, requestNumber =>
        {
            if (guard.TryAllow("client-1", out _))
            {
                Interlocked.Increment(ref allowed);
            }
        });

        Assert.Equal(3, allowed);
    }

    [Fact]
    public void ContactSubmissionAbuseGuard_BoundsTrackedClientPartitions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Contact:RateLimit:MaxRequestsPerWindow"] = "3",
                ["Contact:RateLimit:WindowMinutes"] = "10",
                ["Contact:RateLimit:MaxTrackedClients"] = "1"
            })
            .Build();
        var guard = new ContactSubmissionAbuseGuard(configuration);

        Assert.True(guard.TryAllow("client-1", out _));
        Assert.False(guard.TryAllow("client-2", out var rejectionReason));
        Assert.Equal("rate_limit", rejectionReason);
    }

    [Fact]
    public void ContactSubmissionAbuseGuard_RemovesExpiredClientsWithoutMutatingDuringEnumeration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Contact:RateLimit:MaxRequestsPerWindow"] = "3",
                ["Contact:RateLimit:WindowMinutes"] = "1",
                ["Contact:RateLimit:MaxTrackedClients"] = "3"
            })
            .Build();
        var timeProvider = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var guard = new ContactSubmissionAbuseGuard(configuration, timeProvider);

        Assert.True(guard.TryAllow("client-1", out _));
        Assert.True(guard.TryAllow("client-2", out _));
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        Assert.True(guard.TryAllow("client-3", out _));
    }

    [Fact]
    public void ContactVerificationService_LetsOnlyOneOfManyConcurrentVerificationsSucceed()
    {
        var service = new ContactVerificationService(NullLogger<ContactVerificationService>.Instance, new ConfigurationBuilder().Build());
        var token = service.CreateVerification(new ContactMessage { Name = "A", Email = "a@example.test", Reason = "Personal note", Message = "m" }).Token;
        var successes = 0;

        Parallel.For(0, 200, _ =>
        {
            if (service.Verify(token).IsSuccess)
            {
                Interlocked.Increment(ref successes);
            }
        });

        Assert.Equal(1, successes);
    }

    [Fact]
    public void ContactSubmissionAbuseGuard_LimitsRecipientsIndependentlyOfClientsAndIgnoresCase()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Contact:RateLimit:MaxRequestsPerWindow"] = "10",
                ["Contact:RateLimit:MaxRequestsPerRecipient"] = "2"
            })
            .Build();
        var guard = new ContactSubmissionAbuseGuard(configuration);

        Assert.True(guard.TryAllowRecipient("victim@example.test", out _));
        Assert.True(guard.TryAllowRecipient("VICTIM@example.test ", out _));
        Assert.False(guard.TryAllowRecipient("victim@example.test", out var rejectionReason));
        Assert.Equal("rate_limit", rejectionReason);
        Assert.True(guard.TryAllowRecipient("other@example.test", out _));
        Assert.True(guard.TryAllow("victim@example.test", out _));
    }

}
