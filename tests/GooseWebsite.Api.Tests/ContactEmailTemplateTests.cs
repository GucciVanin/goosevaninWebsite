using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Modules.Contact.Services;
using GooseWebsite.Api.Shared.Email;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Pins the variables each MailerSend template receives (and the transport request), so a missing one fails here instead of
// rendering as its own name in a real inbox.
public sealed class ContactEmailTemplateTests
{
    private const string ConfirmationId = "tpl-confirmation";
    private const string ReceiptId = "tpl-receipt";
    private const string OwnerId = "tpl-owner";

    private static readonly ContactMessageVerification Pending =
        new("Alex Rivera", "alex@example.test", "Work or collaboration", "Hello\nthere", DateTime.UtcNow.AddMinutes(5));

    [Fact]
    public async Task VerificationRequest_UsesConfirmationTemplate_WithItsVariablesAndNeverTheMessage()
    {
        var mailer = new TestMailService();
        var service = CreateService(mailer);

        var delivered = await service.SendVerificationRequestAsync("Alex Rivera", "alex@example.test", "https://site.test/contact/verify#token=abc");

        Assert.True(delivered);
        var email = Assert.Single(mailer.Templated);
        Assert.Equal(ConfirmationId, email.TemplateId);
        Assert.Equal("alex@example.test", email.To.Email);
        Assert.Equal(
            new SortedDictionary<string, string>
            {
                ["logo_url"] = "https://site.test/email/gcv-logo.png",
                ["name"] = "Alex Rivera",
                ["site_url"] = "https://site.test",
                ["verification_url"] = "https://site.test/contact/verify#token=abc"
            },
            new SortedDictionary<string, string>(email.Variables.ToDictionary(pair => pair.Key, pair => pair.Value)));
    }

    [Fact]
    public async Task Delivery_SendsOwnerNoticeThenSenderReceipt_EachWithEveryVariable()
    {
        var mailer = new TestMailService();
        var service = CreateService(mailer);

        var delivered = await service.SendAsync(Pending);

        Assert.Equal(ContactDeliveryOutcome.Delivered, delivered);
        Assert.Equal(2, mailer.Templated.Count);

        var owner = mailer.Templated[0];
        Assert.Equal(OwnerId, owner.TemplateId);
        Assert.Equal("gustavo@example.test", owner.To.Email);
        Assert.Equal("alex@example.test", owner.ReplyTo?.Email);

        var receipt = mailer.Templated[1];
        Assert.Equal(ReceiptId, receipt.TemplateId);
        Assert.Equal("alex@example.test", receipt.To.Email);
        Assert.Equal("gustavo@example.test", receipt.ReplyTo?.Email);

        foreach (var email in mailer.Templated)
        {
            foreach (var variable in new[] { "name", "sender_email", "reason", "message", "site_url", "logo_url" })
            {
                Assert.True(email.Variables.ContainsKey(variable), $"{email.TemplateId} is missing '{variable}'");
                Assert.False(string.IsNullOrWhiteSpace(email.Variables[variable]), $"{email.TemplateId} has an empty '{variable}'");
            }

            Assert.Equal("Alex Rivera", email.Variables["name"]);
            Assert.Equal("alex@example.test", email.Variables["sender_email"]);
            Assert.Equal("Work or collaboration", email.Variables["reason"]);
            Assert.Equal("Hello\nthere", email.Variables["message"]);
        }
    }

    [Fact]
    public async Task Delivery_WhenOwnerNoticeFails_DoesNotSendReceipt()
    {
        var mailer = new TestMailService { ShouldFail = true };

        var delivered = await CreateService(mailer).SendAsync(Pending);

        Assert.Equal(ContactDeliveryOutcome.Failed, delivered);
        Assert.Single(mailer.Templated);
    }

    [Fact]
    public async Task Delivery_WhenReceiptFails_ReportsItAndTheRetryOnlySendsTheReceipt()
    {
        var mailer = new TestMailService { FailTemplateId = ReceiptId };
        var service = CreateService(mailer);

        var first = await service.SendAsync(Pending);

        Assert.Equal(ContactDeliveryOutcome.OwnerNotifiedReceiptFailed, first);
        Assert.Equal([OwnerId, ReceiptId], mailer.Templated.Select(email => email.TemplateId));

        mailer.FailTemplateId = null;
        mailer.Reset();
        var retry = await service.SendAsync(Pending with { OwnerNotified = true });

        Assert.Equal(ContactDeliveryOutcome.Delivered, retry);
        Assert.Equal([ReceiptId], mailer.Templated.Select(email => email.TemplateId));
    }

    [Fact]
    public async Task Delivery_WithoutTemplateIdsOrRecipient_SendsNothing()
    {
        var mailer = new TestMailService();
        var noTemplates = CreateService(mailer, new ContactOptions { RecipientEmail = "gustavo@example.test" });
        var noRecipient = CreateService(mailer, new ContactOptions
        {
            Templates = new ContactEmailTemplates { SenderConfirmation = ConfirmationId, SenderReceipt = ReceiptId, OwnerNotification = OwnerId }
        });

        Assert.Equal(ContactDeliveryOutcome.Failed, await noTemplates.SendAsync(Pending));
        Assert.False(await noTemplates.SendVerificationRequestAsync("Alex", "alex@example.test", "https://site.test/x"));
        Assert.Equal(ContactDeliveryOutcome.Failed, await noRecipient.SendAsync(Pending));
        Assert.Empty(mailer.Templated);
    }

    [Fact]
    public async Task LogoUrl_IsTheSiteAddressUnlessOverridden()
    {
        var mailer = new TestMailService();
        var overridden = new ContactOptions
        {
            LogoUrl = "https://cdn.test/logo.png",
            Templates = new ContactEmailTemplates { SenderConfirmation = ConfirmationId, SenderReceipt = ReceiptId, OwnerNotification = OwnerId }
        };

        await CreateService(mailer).SendAsync(Pending);
        await CreateService(mailer, overridden).SendVerificationRequestAsync("Alex", "alex@example.test", "https://site.test/x");

        Assert.Equal("https://site.test/email/gcv-logo.png", mailer.Templated[0].Variables["logo_url"]);
        Assert.Equal("https://cdn.test/logo.png", mailer.Templated[2].Variables["logo_url"]);
    }

    [Fact]
    public async Task MailerSendTransport_PostsTemplateRequestWithBearerKey()
    {
        var handler = new CapturingHandler(HttpStatusCode.Accepted);
        var client = CreateClient(handler);

        var sent = await client.SendTemplateAsync(new TemplateMail(
            "tpl-1",
            "Subject",
            new MailAddressee("to@example.test", "To Name"),
            new Dictionary<string, string> { ["name"] = "To Name", ["message"] = "line1\nline2" },
            new MailAddressee("reply@example.test", "Reply Name")), CancellationToken.None);

        Assert.Equal(MailFailure.None, sent.Failure);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api.test/v1/email", handler.Uri);
        Assert.Equal("Bearer test-api-key", handler.Authorization);

        using var body = JsonDocument.Parse(handler.Body);
        var root = body.RootElement;
        Assert.Equal("tpl-1", root.GetProperty("template_id").GetString());
        Assert.Equal("Subject", root.GetProperty("subject").GetString());
        Assert.Equal("sender@example.test", root.GetProperty("from").GetProperty("email").GetString());
        Assert.Equal("to@example.test", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("reply@example.test", root.GetProperty("reply_to").GetProperty("email").GetString());
        var personalization = root.GetProperty("personalization")[0];
        Assert.Equal("to@example.test", personalization.GetProperty("email").GetString());
        Assert.Equal("To Name", personalization.GetProperty("data").GetProperty("name").GetString());
        Assert.Equal("line1\nline2", personalization.GetProperty("data").GetProperty("message").GetString());
    }

    [Fact]
    public async Task MailerSendTransport_OmitsReplyToWhenNotGiven()
    {
        var handler = new CapturingHandler(HttpStatusCode.Accepted);

        await CreateClient(handler).SendTemplateAsync(new TemplateMail(
            "tpl-1", "Subject", new MailAddressee("to@example.test", "To"), new Dictionary<string, string>()), CancellationToken.None);

        using var body = JsonDocument.Parse(handler.Body);
        Assert.False(body.RootElement.TryGetProperty("reply_to", out _));
    }

    [Fact]
    public async Task MailerSendTransport_WithoutApiKeyMakesNoRequest()
    {
        var handler = new CapturingHandler(HttpStatusCode.Accepted);
        var client = new MailerSendTransport(
            new HttpClient(handler),
            Options.Create(new MailerSendOptions { FromEmail = "sender@example.test" }));

        var sent = await client.SendTemplateAsync(new TemplateMail(
            "tpl-1", "Subject", new MailAddressee("to@example.test", "To"), new Dictionary<string, string>()), CancellationToken.None);

        Assert.Equal(MailFailure.NotConfigured, sent.Failure);
        Assert.Null(handler.Uri);
    }

    private static ContactEmailDeliveryService CreateService(IMailService mailer, ContactOptions? contact = null, string publicBaseUrl = "https://site.test/") =>
        new(
            mailer,
            Options.Create(contact ?? new ContactOptions
            {
                RecipientEmail = "gustavo@example.test",
                Templates = new ContactEmailTemplates { SenderConfirmation = ConfirmationId, SenderReceipt = ReceiptId, OwnerNotification = OwnerId }
            }),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["PublicBaseUrl"] = publicBaseUrl }).Build(),
            NullLogger<ContactEmailDeliveryService>.Instance);

    private static MailerSendTransport CreateClient(CapturingHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(new MailerSendOptions { ApiKey = "test-api-key", BaseUrl = "https://api.test/v1/", FromEmail = "sender@example.test", FromName = "Sender" }));

    private sealed class CapturingHandler(HttpStatusCode status, string responseBody = "") : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Uri { get; private set; }
        public string? Authorization { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri?.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(responseBody) };
        }
    }
}
