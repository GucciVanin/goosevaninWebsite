using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Controllers;
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
    public async Task ContactController_RejectsHoneypotAndRateLimitedRequests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Contact:RateLimit:MaxRequestsPerWindow"] = "1",
                ["Contact:RateLimit:WindowMinutes"] = "10"
            })
            .Build();

        var controller = new ContactController(
            new StubContactVerificationService(),
            new StubContactEmailDeliveryService(),
            new ContactSubmissionAbuseGuard(configuration),
            NullLogger<ContactController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        var honeypotResult = await controller.Send(new ContactMessage
        {
            Name = "Test User",
            Email = "test@example.com",
            Reason = "Work or collaboration",
            Message = "hello",
            Website = "https://example.com"
        });

        Assert.IsType<BadRequestObjectResult>(honeypotResult);

        var validRequest = new ContactMessage
        {
            Name = "Test User",
            Email = "test@example.com",
            Reason = "Work or collaboration",
            Message = "hello",
            Website = string.Empty
        };

        var firstResult = await controller.Send(validRequest);
        Assert.IsType<OkObjectResult>(firstResult);

        controller.HttpContext.Request.Headers["X-Forwarded-For"] = "198.51.100.25";
        var secondResult = await controller.Send(validRequest);
        var objectResult = Assert.IsType<ObjectResult>(secondResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
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

    [Fact]
    public async Task ContactController_DiscardsPendingMessageWhenVerificationEmailThrows()
    {
        var verification = new StubContactVerificationService();
        var delivery = new StubContactEmailDeliveryService { VerificationShouldThrow = true };
        var controller = new ContactController(
            verification,
            delivery,
            new ContactSubmissionAbuseGuard(new ConfigurationBuilder().Build()),
            NullLogger<ContactController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Send(new ContactMessage
        {
            Name = "Test User",
            Email = "test@example.com",
            Reason = "Personal note",
            Message = "hello"
        });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
        Assert.Equal(["token-123"], verification.DiscardedTokens);
    }

    [Fact]
    public async Task ContactController_DoesNotLogSenderEmailOrMessageBody()
    {
        var logger = new CapturingLogger<ContactController>();
        var controller = new ContactController(
            new StubContactVerificationService(),
            new StubContactEmailDeliveryService(),
            new ContactSubmissionAbuseGuard(new ConfigurationBuilder().Build()),
            logger)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        await controller.Send(new ContactMessage
        {
            Name = "Test User",
            Email = "private-sender@example.com",
            Reason = "Personal note",
            Message = "private contact message body"
        });

        var logOutput = string.Join(Environment.NewLine, logger.Messages);
        Assert.DoesNotContain("private-sender@example.com", logOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private contact message body", logOutput, StringComparison.OrdinalIgnoreCase);
    }

}
