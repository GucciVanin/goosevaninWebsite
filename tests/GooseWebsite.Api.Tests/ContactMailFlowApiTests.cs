using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace GooseWebsite.Api.Tests;

// TE11 end to end: the real contact module with the real delivery service and renderer over the test mail
// service. No MailerSend key and no template ids are configured anywhere in these tests.
public sealed class ContactMailFlowApiTests
{
    private const string Body = "private contact message body <b>bold</b>";

    private static readonly object ValidMessage = new
    {
        name = "Test Sender",
        email = "sender@example.test",
        reason = "Work or collaboration",
        message = Body,
        website = ""
    };

    [Fact]
    public async Task FullFlow_WorksWithoutAnyMailerSendKeyOrTemplateId()
    {
        using var factory = new AuthApiFactory(realContactDelivery: true);
        using var client = factory.CreateHttpsClient();

        var submit = await client.PostAsJsonAsync("/api/contact", ValidMessage);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        // AC-1: exactly one mail, to the sender, without the message; nothing for Gustavo yet.
        var confirmation = Assert.Single(factory.Mail.Rendered);
        Assert.Empty(factory.Mail.Templated);
        Assert.Equal("sender@example.test", confirmation.To.Email);
        Assert.Equal("Confirm your email to send your message to Gustavo", confirmation.Subject);
        Assert.DoesNotContain("private contact message body", confirmation.Html + confirmation.PlainText);
        var match = Regex.Match(confirmation.PlainText, @"https?://\S+/contact/verify#token=(\S+)");
        Assert.True(match.Success);
        var token = match.Groups[1].Value;
        Assert.Contains(WebUtility.HtmlEncode(match.Value), confirmation.Html);

        var verify = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        // AC-2: the notice to Gustavo (Reply-To the sender) then the receipt to the sender (Reply-To Gustavo).
        Assert.Equal(3, factory.Mail.Rendered.Count);
        var notice = factory.Mail.Rendered[1];
        var receipt = factory.Mail.Rendered[2];
        Assert.Equal("gustavo@example.test", notice.To.Email);
        Assert.Equal("sender@example.test", notice.ReplyTo?.Email);
        Assert.Equal("sender@example.test", receipt.To.Email);
        Assert.Equal("gustavo@example.test", receipt.ReplyTo?.Email);
        Assert.Contains(Body, notice.PlainText);
        Assert.Contains(Body, receipt.PlainText);
        Assert.Contains("&lt;b&gt;bold&lt;/b&gt;", notice.Html);
        Assert.Contains("&lt;b&gt;bold&lt;/b&gt;", receipt.Html);

        // AC-8: nothing the visitor wrote, no address, link or token reaches a log line.
        var logs = string.Join('\n', factory.Logs.Messages);
        Assert.DoesNotContain("private contact message body", logs);
        Assert.DoesNotContain("sender@example.test", logs);
        Assert.DoesNotContain("gustavo@example.test", logs);
        Assert.DoesNotContain(token, logs);
        Assert.DoesNotContain("/contact/verify", logs);
    }

    [Fact]
    public async Task Retry_AfterAFailedReceipt_SendsOnlyTheReceipt()
    {
        using var factory = new AuthApiFactory(realContactDelivery: true);
        using var client = factory.CreateHttpsClient();
        await client.PostAsJsonAsync("/api/contact", ValidMessage);
        var token = Regex.Match(Assert.Single(factory.Mail.Rendered).PlainText, @"token=(\S+)").Groups[1].Value;
        factory.Mail.Reset();
        factory.Mail.FailRecipients.Add("sender@example.test");

        var failed = await client.PostAsJsonAsync("/api/contact/verify", new { token });
        Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);
        Assert.Equal(["gustavo@example.test"], factory.Mail.Rendered.Select(mail => mail.To.Email));

        factory.Mail.FailRecipients.Clear();
        factory.Mail.Reset();
        var retry = await client.PostAsJsonAsync("/api/contact/verify", new { token });

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(["sender@example.test"], factory.Mail.Rendered.Select(mail => mail.To.Email));
    }

    [Fact]
    public async Task Submit_WhenTheConfirmationMailFails_AnswersBadGatewayAndSendsNothingToGustavo()
    {
        using var factory = new AuthApiFactory(realContactDelivery: true);
        using var client = factory.CreateHttpsClient();
        factory.Mail.ShouldFail = true;

        var response = await client.PostAsJsonAsync("/api/contact", ValidMessage);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Empty(factory.Mail.Rendered);
    }
}
