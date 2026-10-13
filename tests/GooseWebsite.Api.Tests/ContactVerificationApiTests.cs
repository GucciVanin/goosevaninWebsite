using System.Net;
using System.Net.Http.Json;
using System.Web;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Proves the contact message is delivered only after the sender confirms the emailed link.
public sealed class ContactVerificationApiTests
{
    private static readonly object ValidMessage = new
    {
        name = "Test Sender",
        email = "sender@example.test",
        reason = "Work or collaboration",
        message = "private contact message body",
        website = ""
    };

    [Fact]
    public async Task Submit_EmailsLinkToSender_AndNeverReturnsTokenOrUrl()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/contact", ValidMessage);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.Single(factory.ContactEmail.VerificationRequests);
        Assert.Equal("sender@example.test", request.Email);
        var token = ExtractToken(request.Url);
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.DoesNotContain(token, body, StringComparison.Ordinal);
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/contact/verify", body, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.ContactEmail.DeliveredMessages);
    }

    [Fact]
    public async Task Verify_DeliversOnceOnPost_AndRejectsReplayAndGet()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();
        await client.PostAsJsonAsync("/api/contact", ValidMessage);
        var token = ExtractToken(Assert.Single(factory.ContactEmail.VerificationRequests).Url);

        // A scanner fetching the link (or the old GET route) must never trigger delivery.
        _ = await client.GetAsync($"/api/contact/verify?token={Uri.EscapeDataString(token)}");
        Assert.Empty(factory.ContactEmail.DeliveredMessages);

        var confirmed = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        Assert.Single(factory.ContactEmail.DeliveredMessages);

        var replay = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.Single(factory.ContactEmail.DeliveredMessages);
    }

    [Fact]
    public async Task Verify_RejectsUnknownToken()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/contact/verify", new { token = "not-a-real-token" });

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(factory.ContactEmail.DeliveredMessages);
    }

    [Fact]
    public async Task Submit_WhenVerificationEmailFails_ReturnsBadGatewayAndSendsNothing()
    {
        using var factory = new AuthApiFactory();
        factory.ContactEmail.VerificationShouldFail = true;
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/contact", ValidMessage);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Empty(factory.ContactEmail.VerificationRequests);
        Assert.Empty(factory.ContactEmail.DeliveredMessages);
    }

    [Fact]
    public async Task Verify_WhenDeliveryFails_KeepsTheMessageSoTheSameLinkCanBeRetried()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();
        await client.PostAsJsonAsync("/api/contact", ValidMessage);
        var token = ExtractToken(Assert.Single(factory.ContactEmail.VerificationRequests).Url);

        factory.ContactEmail.DeliveryShouldFail = true;
        var failed = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);
        Assert.Empty(factory.ContactEmail.DeliveredMessages);

        factory.ContactEmail.DeliveryShouldFail = false;
        var retried = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        Assert.Single(factory.ContactEmail.DeliveredMessages);

        var replay = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
    }

    [Fact]
    public async Task Submit_LimitsVerificationEmailsPerRecipientAddress_IgnoringCase()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();

        var first = await client.PostAsJsonAsync("/api/contact", ValidMessage);
        var second = await client.PostAsJsonAsync("/api/contact", new { name = "Other", email = "SENDER@example.test", reason = "Personal note", message = "again", website = "" });
        var third = await client.PostAsJsonAsync("/api/contact", ValidMessage);
        var otherAddress = await client.PostAsJsonAsync("/api/contact", new { name = "Else", email = "someone-else@example.test", reason = "Personal note", message = "hi", website = "" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherAddress.StatusCode);
        Assert.Equal(3, factory.ContactEmail.VerificationRequests.Count);
    }

    [Fact]
    public async Task Verify_WhenOnlyTheReceiptFails_TheRetryDoesNotNotifyGustavoAgain()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();
        await client.PostAsJsonAsync("/api/contact", ValidMessage);
        var token = ExtractToken(Assert.Single(factory.ContactEmail.VerificationRequests).Url);

        factory.ContactEmail.ReceiptShouldFail = true;
        var failed = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);

        factory.ContactEmail.ReceiptShouldFail = false;
        var retried = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);

        Assert.Equal([false, true], factory.ContactEmail.OwnerNotifiedSeen);
        Assert.Single(factory.ContactEmail.DeliveredMessages);
    }

    [Fact]
    public async Task Verify_ConcurrentConfirmationsDeliverExactlyOnce()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();
        await client.PostAsJsonAsync("/api/contact", ValidMessage);
        var token = ExtractToken(Assert.Single(factory.ContactEmail.VerificationRequests).Url);

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => client.PostAsJsonAsync("/api/contact/verify", new { token })));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Single(factory.ContactEmail.DeliveredMessages);
    }

    private static string ExtractToken(string url) =>
        HttpUtility.ParseQueryString(new Uri(url).Fragment.TrimStart('#'))["token"] ?? string.Empty;
}
