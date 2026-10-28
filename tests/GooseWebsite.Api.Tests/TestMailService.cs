using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GooseWebsite.Api.Shared.Email;

namespace GooseWebsite.Api.Tests;

// The one mail double for tests: records everything handed to the shared mail interface and never
// contacts SMTP or MailerSend.
internal sealed class TestMailService : IMailService
{
    private readonly ConcurrentQueue<RenderedMail> _rendered = new();
    private readonly ConcurrentQueue<TemplateMail> _templated = new();

    public IReadOnlyList<RenderedMail> Rendered => _rendered.ToArray();

    public IReadOnlyList<TemplateMail> Templated => _templated.ToArray();

    /// <summary>When true every send fails and nothing is recorded.</summary>
    public bool ShouldFail { get; set; }

    /// <summary>When true every send reports that the provider is not configured.</summary>
    public bool Unconfigured { get; set; }

    /// <summary>Template sends with this id fail (and are still recorded).</summary>
    public string? FailTemplateId { get; set; }

    /// <summary>The rendered mails as account tests read them: recipient, display name and the first link in the plain text.</summary>
    public IReadOnlyCollection<(string Email, string DisplayName, string Url)> AccountMessages =>
        _rendered.Select(mail => (mail.To.Email, mail.To.Name, Regex.Match(mail.PlainText, @"https?://\S+").Value)).ToArray();

    /// <summary>Forgets everything recorded so far.</summary>
    public void Reset()
    {
        _rendered.Clear();
        _templated.Clear();
    }

    public Task<MailResult> SendAsync(RenderedMail mail, CancellationToken cancellationToken = default)
    {
        if (Unconfigured)
        {
            return Task.FromResult(MailResult.NotConfigured);
        }

        if (ShouldFail)
        {
            return Task.FromResult(MailResult.Failed(MailFailure.Rejected));
        }

        _rendered.Enqueue(mail);
        return Task.FromResult(MailResult.Success);
    }

    public Task<MailResult> SendTemplateAsync(TemplateMail mail, CancellationToken cancellationToken = default)
    {
        if (Unconfigured)
        {
            return Task.FromResult(MailResult.NotConfigured);
        }

        // Template attempts are recorded even when they fail, so tests can see what was tried.
        _templated.Enqueue(mail);
        return Task.FromResult(ShouldFail || mail.TemplateId == FailTemplateId ? MailResult.Failed(MailFailure.Rejected) : MailResult.Success);
    }
}
