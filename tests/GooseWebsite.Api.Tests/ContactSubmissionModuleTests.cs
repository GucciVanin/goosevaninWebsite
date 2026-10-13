using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Modules.Contact.Services;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Exercises the Contact submission module through its own interface: submit a Contact submission,
// then complete Verification. Request-level behavior stays covered by ContactVerificationApiTests.
public sealed class ContactSubmissionModuleTests
{
    private static ContactMessage ValidSubmission(string email = "test@example.com") => new()
    {
        Name = "Test User",
        Email = email,
        Reason = "Personal note",
        Message = "private contact message body"
    };

    private static ContactSubmissionService CreateModule(
        StubContactEmailDeliveryService delivery,
        Dictionary<string, string?>? settings = null,
        Microsoft.Extensions.Logging.ILogger<ContactSubmissionService>? logger = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();
        return new ContactSubmissionService(
            new ContactVerificationService(NullLogger<ContactVerificationService>.Instance, configuration),
            delivery,
            new ContactSubmissionAbuseGuard(configuration),
            logger ?? NullLogger<ContactSubmissionService>.Instance);
    }

    private static string TokenFrom(string url) =>
        HttpUtility.ParseQueryString(new Uri(url).Fragment.TrimStart('#'))["token"] ?? string.Empty;

    [Fact]
    public async Task Submit_RejectsHoneypotWithoutSendingEmail()
    {
        var delivery = new StubContactEmailDeliveryService();
        var module = CreateModule(delivery);
        var submission = ValidSubmission();
        submission.Website = "https://example.com";

        var outcome = await module.SubmitAsync(submission, "client-1");

        Assert.Equal(ContactSubmitStatus.Honeypot, outcome.Status);
        Assert.Empty(delivery.VerificationRequests);
    }

    [Fact]
    public async Task Submit_LimitsSubmissionsPerClientKey()
    {
        var module = CreateModule(
            new StubContactEmailDeliveryService(),
            new() { ["Contact:RateLimit:MaxRequestsPerWindow"] = "1" });

        var first = await module.SubmitAsync(ValidSubmission("a@example.test"), "client-1");
        var second = await module.SubmitAsync(ValidSubmission("b@example.test"), "client-1");

        Assert.Equal(ContactSubmitStatus.Accepted, first.Status);
        Assert.Equal(ContactSubmitStatus.ClientRateLimited, second.Status);
    }

    [Fact]
    public async Task Submit_WhenTheMailLibraryRejectsTheAddress_DiscardsThePendingMessage()
    {
        var delivery = new StubContactEmailDeliveryService { VerificationShouldThrow = true };
        var module = CreateModule(delivery);

        var outcome = await module.SubmitAsync(ValidSubmission(), "client-1");

        Assert.Equal(ContactSubmitStatus.EmailNotSent, outcome.Status);
        var attempted = Assert.Single(delivery.AttemptedVerificationUrls);
        var verified = await module.VerifyAsync(TokenFrom(attempted));
        Assert.Equal(ContactVerifyStatus.UnknownOrAlreadyUsed, verified.Status);
        Assert.Empty(delivery.DeliveredMessages);
    }

    [Fact]
    public async Task Verify_WhenDeliveryFails_KeepsThePendingMessageForTheSameLink()
    {
        var delivery = new StubContactEmailDeliveryService();
        var module = CreateModule(delivery);
        await module.SubmitAsync(ValidSubmission(), "client-1");
        var token = TokenFrom(Assert.Single(delivery.VerificationRequests).Url);

        delivery.DeliveryShouldFail = true;
        var failed = await module.VerifyAsync(token);
        delivery.DeliveryShouldFail = false;
        var retried = await module.VerifyAsync(token);
        var replay = await module.VerifyAsync(token);

        Assert.Equal(ContactVerifyStatus.DeliveryFailed, failed.Status);
        Assert.Equal(ContactVerifyStatus.Delivered, retried.Status);
        Assert.Equal(ContactVerifyStatus.UnknownOrAlreadyUsed, replay.Status);
        Assert.Single(delivery.DeliveredMessages);
    }

    [Fact]
    public async Task Verify_WhenOnlyTheReceiptFails_TheRetryRemembersGustavoWasNotified()
    {
        var delivery = new StubContactEmailDeliveryService();
        var module = CreateModule(delivery);
        await module.SubmitAsync(ValidSubmission(), "client-1");
        var token = TokenFrom(Assert.Single(delivery.VerificationRequests).Url);

        delivery.ReceiptShouldFail = true;
        await module.VerifyAsync(token);
        delivery.ReceiptShouldFail = false;
        var retried = await module.VerifyAsync(token);

        Assert.Equal(ContactVerifyStatus.Delivered, retried.Status);
        Assert.Equal([false, true], delivery.OwnerNotifiedSeen);
    }

    [Fact]
    public async Task Submit_DoesNotLogSenderEmailOrMessageBody()
    {
        var logger = new CapturingLogger<ContactSubmissionService>();
        var module = CreateModule(new StubContactEmailDeliveryService(), logger: logger);

        await module.SubmitAsync(ValidSubmission("private-sender@example.com"), "client-1");

        var logOutput = string.Join(Environment.NewLine, logger.Messages);
        Assert.DoesNotContain("private-sender@example.com", logOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private contact message body", logOutput, StringComparison.OrdinalIgnoreCase);
    }
}
