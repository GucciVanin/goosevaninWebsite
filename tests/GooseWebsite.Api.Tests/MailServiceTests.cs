using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Shared.Email;
using Xunit;

namespace GooseWebsite.Api.Tests;

// The shared mail service is the one place the privacy logging rule is enforced: whatever happens, no
// address, link, body, key or provider response text reaches a log line, and callers never see an exception.
public sealed class MailServiceTests
{
    private const string Address = "secret-recipient@example.test";
    private const string Link = "https://site.test/verify-email#userId=u1&token=SECRETTOKEN";
    private const string Body = "very private body text";
    private const string ApiKey = "test-api-key-value";
    private static readonly string[] Secrets = [Address, "SECRETTOKEN", Link, Body, ApiKey, "Secret Person"];

    private static readonly MailAddressee To = new(Address, "Secret Person");
    private static readonly RenderedMail Rendered = new(To, "Subject", $"<p>{Body} {Link}</p>", $"{Body} {Link}");
    private static readonly TemplateMail Template = new("tpl-1", "Subject", To, new Dictionary<string, string> { ["message"] = Body, ["verification_url"] = Link });

    private sealed class ThrowingTransport(Exception exception) : IRenderedMailTransport, ITemplateMailTransport
    {
        public string Name => "throwing";
        public Task<MailTransportOutcome> SendAsync(RenderedMail mail, CancellationToken cancellationToken) => throw exception;
        public Task<MailTransportOutcome> SendTemplateAsync(TemplateMail mail, CancellationToken cancellationToken) => throw exception;
    }

    private sealed class AcceptingTransport : IRenderedMailTransport, ITemplateMailTransport
    {
        public int Calls { get; private set; }
        public string Name => "accepting";
        public Task<MailTransportOutcome> SendAsync(RenderedMail mail, CancellationToken cancellationToken) { Calls++; return Task.FromResult(MailTransportOutcome.Accepted); }
        public Task<MailTransportOutcome> SendTemplateAsync(TemplateMail mail, CancellationToken cancellationToken) { Calls++; return Task.FromResult(MailTransportOutcome.Accepted); }
    }

    private sealed class StatusHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private static MailerSendTransport MailerSend(HttpStatusCode status = HttpStatusCode.Accepted, string body = "", bool configured = true) =>
        new(new HttpClient(new StatusHandler(status, body)),
            Options.Create(new MailerSendOptions { ApiKey = configured ? ApiKey : "", BaseUrl = "https://api.test/v1/", FromEmail = "sender@example.test" }));

    private static SmtpMailTransport Smtp(bool configured) =>
        new(Options.Create(configured
            ? new SmtpOptions { Host = "127.0.0.1", Port = 1, Username = "u", Password = "p", FromEmail = "sender@example.test" }
            : new SmtpOptions()));

    private static MailService Service(CapturingLogger<MailService> logger, IRenderedMailTransport rendered, ITemplateMailTransport template) =>
        new(rendered, template, logger);

    private static void AssertNothingSecretLogged(CapturingLogger<MailService> logger)
    {
        Assert.NotEmpty(logger.Messages);
        var log = string.Join(Environment.NewLine, logger.Messages);
        foreach (var secret in Secrets)
        {
            Assert.DoesNotContain(secret, log, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TemplateSend_Succeeds_AndLogsNoAddressLinkBodyOrKey()
    {
        var logger = new CapturingLogger<MailService>();
        var service = Service(logger, Smtp(false), MailerSend());

        var result = await service.SendTemplateAsync(Template);

        Assert.True(result.Succeeded);
        Assert.Equal(MailFailure.None, result.Failure);
        AssertNothingSecretLogged(logger);
    }

    [Fact]
    public async Task TemplateSend_WhenProviderRejects_ReturnsFailureAndLogsOnlyTheStatus()
    {
        var logger = new CapturingLogger<MailService>();
        var service = Service(logger, Smtp(false), MailerSend(HttpStatusCode.UnprocessableEntity, $"{{\"message\":\"bad {Address} {Body}\"}}"));

        var result = await service.SendTemplateAsync(Template);

        Assert.False(result.Succeeded);
        Assert.Equal(MailFailure.Rejected, result.Failure);
        Assert.Contains("422", string.Join(' ', logger.Messages));
        AssertNothingSecretLogged(logger);
    }

    [Fact]
    public async Task TemplateSend_WhenNotConfigured_ReportsNotConfiguredAndLogsNoSecrets()
    {
        var logger = new CapturingLogger<MailService>();
        var service = Service(logger, Smtp(false), MailerSend(configured: false));

        var result = await service.SendTemplateAsync(Template);

        Assert.Equal(MailFailure.NotConfigured, result.Failure);
        AssertNothingSecretLogged(logger);
    }

    [Fact]
    public async Task RenderedSend_WhenNotConfigured_ReportsNotConfiguredAndLogsNoSecrets()
    {
        var logger = new CapturingLogger<MailService>();
        var service = Service(logger, Smtp(false), MailerSend());

        var result = await service.SendAsync(Rendered);

        Assert.Equal(MailFailure.NotConfigured, result.Failure);
        AssertNothingSecretLogged(logger);
    }

    [Fact]
    public async Task RenderedSend_WhenTheConnectionFails_ReturnsFailureWithoutThrowingAndLogsNoSecrets()
    {
        var logger = new CapturingLogger<MailService>();
        var service = Service(logger, Smtp(true), MailerSend());

        var result = await service.SendAsync(Rendered);

        Assert.False(result.Succeeded);
        Assert.Equal(MailFailure.TransportError, result.Failure);
        AssertNothingSecretLogged(logger);
    }

    [Fact]
    public async Task WhenATransportAccepts_BothKindsSucceedAndNothingSecretIsLogged()
    {
        var logger = new CapturingLogger<MailService>();
        var accepting = new AcceptingTransport();
        var service = Service(logger, accepting, accepting);

        Assert.True((await service.SendAsync(Rendered)).Succeeded);
        Assert.True((await service.SendTemplateAsync(Template)).Succeeded);
        Assert.Equal(2, accepting.Calls);
        AssertNothingSecretLogged(logger);
    }

    [Fact]
    public async Task ATransportThatThrows_NeverThrowsToTheCaller_AndTheExceptionTextIsNotLogged()
    {
        var logger = new CapturingLogger<MailService>();
        var boom = new ThrowingTransport(new InvalidOperationException($"failed for {Address} with {Link}"));
        var service = Service(logger, boom, boom);

        var rendered = await service.SendAsync(Rendered);
        var templated = await service.SendTemplateAsync(Template);

        Assert.Equal(MailFailure.TransportError, rendered.Failure);
        Assert.Equal(MailFailure.TransportError, templated.Failure);
        Assert.Contains("InvalidOperationException", string.Join(' ', logger.Messages));
        AssertNothingSecretLogged(logger);
    }

    [Fact]
    public void NoModuleTalksToSmtpOrMailerSendDirectly()
    {
        string? root = null;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "GooseWebsite.Api", "Modules")))
            {
                root = Path.Combine(dir.FullName, "src", "GooseWebsite.Api", "Modules");
                break;
            }
        }

        Assert.NotNull(root);
        string[] forbidden = ["System.Net.Mail", "SmtpClient", "MailerSendTransport", "SmtpMailTransport", "IRenderedMailTransport", "ITemplateMailTransport", "mailersend.com"];
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => forbidden.Any(word => File.ReadAllText(file).Contains(word, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void MailerSendSettings_ComeFromTheEmailSection_AndTheOlderSectionStillWorks()
    {
        static MailerSendOptions Bind(Dictionary<string, string?> settings)
        {
            var services = new ServiceCollection().AddLogging();
            services.AddEmail(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
            return services.BuildServiceProvider().GetRequiredService<IOptions<MailerSendOptions>>().Value;
        }

        var legacy = Bind(new() { ["MailerSend:ApiKey"] = "legacy-key", ["MailerSend:FromEmail"] = "a@example.test" });
        Assert.Equal("legacy-key", legacy.ApiKey);

        var current = Bind(new() { ["MailerSend:ApiKey"] = "legacy-key", ["Email:MailerSend:ApiKey"] = "new-key", ["Email:MailerSend:FromEmail"] = "b@example.test" });
        Assert.Equal("new-key", current.ApiKey);
        Assert.Equal("b@example.test", current.FromEmail);
    }
}
