using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Modules.Contact.Services;
using GooseWebsite.Api.Shared.Email;
using Xunit;

namespace GooseWebsite.Api.Tests;

// TE11: the contact emails are rendered by the site and sent as finished mail through IMailService.SendAsync.
// No test here contacts SMTP or MailerSend; TestMailService records what would be sent.
public sealed class ContactRenderedMailTests
{
    private const string Hostile = "<script>alert(1)</script> & \"quotes\" 'single' <b>x</b>";

    private static readonly ContactMessageVerification Pending =
        new("Alex Rivera", "alex@example.test", "Work or collaboration", "Hello\nthere", DateTime.UtcNow.AddMinutes(5));

    // ---- AC-1 ----

    [Fact]
    public async Task VerificationRequest_SendsOneRenderedMailToTheSender_WithTheLinkAndNeverTheMessage()
    {
        var mailer = new TestMailService();

        var delivered = await CreateService(mailer).SendVerificationRequestAsync("Alex Rivera", "alex@example.test", "https://site.test/contact/verify#token=abc");

        Assert.True(delivered);
        var mail = Assert.Single(mailer.Rendered);
        Assert.Empty(mailer.Templated);
        Assert.Equal("alex@example.test", mail.To.Email);
        Assert.Equal("Confirm your email to send your message to Gustavo", mail.Subject);
        Assert.Contains("https://site.test/contact/verify#token=abc", mail.Html);
        Assert.Contains("https://site.test/contact/verify#token=abc", mail.PlainText);
        Assert.Contains("Alex Rivera", mail.Html);
    }

    [Fact]
    public async Task VerificationRequest_WhenTheMailFails_ReturnsFalse()
    {
        var mailer = new TestMailService { ShouldFail = true };

        Assert.False(await CreateService(mailer).SendVerificationRequestAsync("Alex", "alex@example.test", "https://site.test/x"));
    }

    // ---- AC-2 ----

    [Fact]
    public async Task Delivery_SendsOwnerNoticeThenSenderReceipt_EachWithHtmlPlainTextAndReplyTo()
    {
        var mailer = new TestMailService();

        var outcome = await CreateService(mailer).SendAsync(Pending);

        Assert.Equal(ContactDeliveryOutcome.Delivered, outcome);
        Assert.Empty(mailer.Templated);
        Assert.Equal(2, mailer.Rendered.Count);

        var notice = mailer.Rendered[0];
        Assert.Equal("gustavo@example.test", notice.To.Email);
        Assert.Equal("New contact message from Alex Rivera", notice.Subject);
        Assert.Equal("alex@example.test", notice.ReplyTo?.Email);
        Assert.Equal("Alex Rivera", notice.ReplyTo?.Name);
        foreach (var part in new[] { notice.Html, notice.PlainText })
        {
            Assert.Contains("Alex Rivera", part);
            Assert.Contains("alex@example.test", part);
            Assert.Contains("Work or collaboration", part);
            Assert.Contains("Hello", part);
            Assert.Contains("there", part);
        }

        var receipt = mailer.Rendered[1];
        Assert.Equal("alex@example.test", receipt.To.Email);
        Assert.Equal("Your message has been received", receipt.Subject);
        Assert.Equal("gustavo@example.test", receipt.ReplyTo?.Email);
        Assert.Contains("Hello\nthere", receipt.PlainText);
        Assert.Contains("Work or collaboration", receipt.PlainText);
        Assert.Contains("Work or collaboration", receipt.Html);
        Assert.Contains("Hello", receipt.Html);
    }

    [Fact]
    public async Task MessageKeepsItsLineBreaksInTheHtml()
    {
        var mailer = new TestMailService();

        await CreateService(mailer).SendAsync(Pending with { Message = "line one\r\nline two\nline three" });

        foreach (var mail in mailer.Rendered)
        {
            Assert.Matches(@"line one\s*<br\s*/?>\s*line two\s*<br\s*/?>\s*line three", mail.Html);
        }
    }

    // ---- AC-3 ----

    [Fact]
    public async Task VisitorValues_AreEscapedInTheHtml_AndUnchangedInThePlainText()
    {
        var mailer = new TestMailService();
        var service = CreateService(mailer);
        var hostile = Pending with { Name = Hostile, Reason = Hostile, Message = Hostile };

        await service.SendVerificationRequestAsync(Hostile, "alex@example.test", "https://site.test/x");
        await service.SendAsync(hostile);

        Assert.Equal(3, mailer.Rendered.Count);
        foreach (var mail in mailer.Rendered)
        {
            Assert.DoesNotContain("<script>", mail.Html);
            Assert.DoesNotContain("<b>x</b>", mail.Html);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", mail.Html);
            Assert.Contains("&amp;", mail.Html);
            Assert.Contains("&lt;b&gt;x&lt;/b&gt;", mail.Html);
            Assert.DoesNotContain("&lt;", mail.PlainText);
            Assert.DoesNotContain("&amp;", mail.PlainText);
        }

        // The confirmation mail carries the name only; the notice and the receipt carry the message too.
        Assert.Contains(Hostile, mailer.Rendered[1].PlainText);
        Assert.Contains(Hostile, mailer.Rendered[2].PlainText);
    }

    [Fact]
    public async Task SenderEmail_IsEncodedInTheNoticeHtml()
    {
        var mailer = new TestMailService();

        await CreateService(mailer).SendAsync(Pending with { Email = "a\"onmouseover=\"x@example.test" });

        Assert.DoesNotContain("a\"onmouseover", mailer.Rendered[0].Html);
    }

    // ---- AC-4 ----

    [Fact]
    public async Task NamesWithLineBreaks_CannotAddHeadersOrRecipients()
    {
        var mailer = new TestMailService();
        var evil = "Eve\r\nBcc: victim@example.test\nX-Injected: 1";
        var service = CreateService(mailer);

        await service.SendVerificationRequestAsync(evil, "alex@example.test", "https://site.test/x");
        await service.SendAsync(Pending with { Name = evil });

        Assert.Equal(3, mailer.Rendered.Count);
        foreach (var mail in mailer.Rendered)
        {
            Assert.DoesNotContain('\r', mail.Subject);
            Assert.DoesNotContain('\n', mail.Subject);
            Assert.DoesNotContain('\r', mail.To.Name);
            Assert.DoesNotContain('\n', mail.To.Name);
            if (mail.ReplyTo is not null)
            {
                Assert.DoesNotContain('\r', mail.ReplyTo.Name);
                Assert.DoesNotContain('\n', mail.ReplyTo.Name);
            }

            // The framework builds the message without error and with the intended recipients only.
            using var message = SmtpMailTransport.CreateMessage(mail, "from@example.test", "From");
            Assert.Single(message.To);
            Assert.Empty(message.Bcc);
            Assert.Empty(message.CC);
            Assert.Null(message.Headers["X-Injected"]);
            Assert.Null(message.Headers["Bcc"]);
        }
    }

    [Fact]
    public void CreateMessage_WithAHostileDisplayName_DoesNotThrowAndAddsNoHeader()
    {
        var mail = new RenderedMail(
            new MailAddressee("alex@example.test", "Eve\r\nBcc: victim@example.test"),
            "Subject",
            "<p>x</p>",
            "x",
            ReplyTo: new MailAddressee("gustavo@example.test", "G\r\nX-Injected: 1"));

        using var message = SmtpMailTransport.CreateMessage(mail, "from@example.test", "From");

        Assert.Single(message.To);
        Assert.Empty(message.Bcc);
        Assert.Null(message.Headers["Bcc"]);
        Assert.Null(message.Headers["X-Injected"]);
    }

    // ---- AC-5 ----

    [Fact]
    public async Task Delivery_WhenTheOwnerNoticeFails_RestoresWithNothingSentToTheSender()
    {
        var mailer = new TestMailService();
        mailer.FailRecipients.Add("gustavo@example.test");

        var outcome = await CreateService(mailer).SendAsync(Pending);

        Assert.Equal(ContactDeliveryOutcome.Failed, outcome);
        Assert.Empty(mailer.Rendered);
    }

    [Fact]
    public async Task Delivery_WhenTheReceiptFails_ReportsItAndTheRetryOnlySendsTheReceipt()
    {
        var mailer = new TestMailService();
        mailer.FailRecipients.Add("alex@example.test");
        var service = CreateService(mailer);

        var first = await service.SendAsync(Pending);

        Assert.Equal(ContactDeliveryOutcome.OwnerNotifiedReceiptFailed, first);
        Assert.Equal(["gustavo@example.test"], mailer.Rendered.Select(mail => mail.To.Email));

        mailer.FailRecipients.Clear();
        mailer.Reset();
        var retry = await service.SendAsync(Pending with { OwnerNotified = true });

        Assert.Equal(ContactDeliveryOutcome.Delivered, retry);
        Assert.Equal(["alex@example.test"], mailer.Rendered.Select(mail => mail.To.Email));
    }

    // ---- AC-7 (service level): only the recipient is needed ----

    [Fact]
    public async Task Delivery_NeedsOnlyTheRecipientAddress_NoTemplateIdsNoApiKey()
    {
        var mailer = new TestMailService();
        var service = CreateService(mailer, new ContactOptions { RecipientEmail = "gustavo@example.test" });

        Assert.Equal(ContactDeliveryOutcome.Delivered, await service.SendAsync(Pending));
        Assert.True(await service.SendVerificationRequestAsync("Alex", "alex@example.test", "https://site.test/x"));
    }

    [Fact]
    public async Task Delivery_WithoutARecipient_SendsNothing()
    {
        var mailer = new TestMailService();
        var service = CreateService(mailer, new ContactOptions());

        Assert.Equal(ContactDeliveryOutcome.Failed, await service.SendAsync(Pending));
        Assert.Empty(mailer.Rendered);
    }

    [Fact]
    public async Task LogoUrl_IsTheSiteAddressUnlessOverridden()
    {
        var mailer = new TestMailService();

        await CreateService(mailer).SendVerificationRequestAsync("Alex", "alex@example.test", "https://site.test/x");
        await CreateService(mailer, new ContactOptions { RecipientEmail = "g@example.test", LogoUrl = "https://cdn.test/logo.png" })
            .SendVerificationRequestAsync("Alex", "alex@example.test", "https://site.test/x");

        Assert.Contains("https://site.test/email/gcv-logo.png", mailer.Rendered[0].Html);
        Assert.Contains("https://cdn.test/logo.png", mailer.Rendered[1].Html);
    }

    // ---- AC-6 ----

    [Fact]
    public void CreateMessage_WritesReplyToWhenGiven_AndNoneWhenAbsent()
    {
        var withReplyTo = new RenderedMail(
            new MailAddressee("to@example.test", "To"), "S", "<p>x</p>", "x",
            ReplyTo: new MailAddressee("reply@example.test", "Reply Name"));
        var without = withReplyTo with { ReplyTo = null };

        using var a = SmtpMailTransport.CreateMessage(withReplyTo, "from@example.test", "From");
        using var b = SmtpMailTransport.CreateMessage(without, "from@example.test", "From");

        Assert.Equal("reply@example.test", Assert.Single(a.ReplyToList).Address);
        Assert.Equal("Reply Name", a.ReplyToList[0].DisplayName);
        Assert.Empty(b.ReplyToList);
    }

    [Fact]
    public void RenderedMail_ReplyToDefaultsToNone_SoExistingMailIsUnchanged()
    {
        var mail = new RenderedMail(new MailAddressee("to@example.test", "To"), "S", "<p>x</p>", "x");

        Assert.Null(mail.ReplyTo);
        using var message = SmtpMailTransport.CreateMessage(mail, "from@example.test", "From");
        Assert.Empty(message.ReplyToList);
    }

    // ---- Architecture ----

    [Fact]
    public void ContactDoesNotReferenceSubscriptions_AndTheSharedLayoutLivesInShared()
    {
        var root = FindModulesRoot();
        var files = Directory.EnumerateFiles(Path.Combine(root, "Contact"), "*.cs", SearchOption.AllDirectories);

        Assert.All(files, file => Assert.DoesNotContain("Modules.Subscriptions", File.ReadAllText(file)));
        Assert.True(File.Exists(Path.Combine(root, "..", "Shared", "Email", "BrandedEmailLayout.cs")));
        Assert.False(File.Exists(Path.Combine(root, "Subscriptions", "Services", "BrandedEmailLayout.cs")));
    }

    private static string FindModulesRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "GooseWebsite.Api", "Modules");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Modules folder not found.");
    }

    private static ContactEmailDeliveryService CreateService(IMailService mailer, ContactOptions? contact = null, string publicBaseUrl = "https://site.test/") =>
        new(
            mailer,
            Options.Create(contact ?? new ContactOptions { RecipientEmail = "gustavo@example.test" }),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["PublicBaseUrl"] = publicBaseUrl }).Build(),
            NullLogger<ContactEmailDeliveryService>.Instance);
}
