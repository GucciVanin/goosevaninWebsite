using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VaninWebsite.Api.Modules.Contact.Contracts;
using VaninWebsite.Api.Modules.Contact.Controllers;
using VaninWebsite.Api.Modules.Contact.Models;
using VaninWebsite.Api.Modules.Contact.Services;
using Xunit;

namespace VaninWebsite.Api.Tests;

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
    public void ContactController_RejectsHoneypotAndRateLimitedRequests()
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

        var honeypotResult = controller.Send(new ContactMessage
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

        var firstResult = controller.Send(validRequest);
        Assert.IsType<OkObjectResult>(firstResult);

        controller.HttpContext.Request.Headers["X-Forwarded-For"] = "198.51.100.25";
        var secondResult = controller.Send(validRequest);
        var objectResult = Assert.IsType<ObjectResult>(secondResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
    }

    [Fact]
    public void ContactController_DoesNotLogSenderEmailOrMessageBody()
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

        controller.Send(new ContactMessage
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
