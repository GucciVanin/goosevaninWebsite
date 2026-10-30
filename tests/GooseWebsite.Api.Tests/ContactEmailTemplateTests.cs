using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Shared.Email;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Pins the MailerSend transport request. Contact no longer uses templates (TE11); this file and the transport go in TE12.
public sealed class ContactEmailTemplateTests
{
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
