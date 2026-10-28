using System.Text.RegularExpressions;
using GooseWebsite.Api.Modules.Accounts.Services;
using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GooseWebsite.Api.Tests;

// The account verification email is a multipart message: branded HTML plus a readable plain-text alternative.
public sealed class AccountVerificationEmailTests
{
    private const string Link = "https://gustavo.example/verify-email#userId=u1&token=t%2B1";

    private static PublicLinkBuilder Links(string? baseUrl = "https://gustavo.example/")
    {
        var settings = new Dictionary<string, string?>();
        if (baseUrl is not null)
        {
            settings["PublicBaseUrl"] = baseUrl;
        }

        return new PublicLinkBuilder(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    private static async Task<(string To, string Subject, string Plain, string Html)> SendAsync(string displayName = "Ada")
    {
        var sink = new TestMailService();
        var sender = new AccountEmailSender(sink, Links());

        Assert.True(await sender.SendVerificationEmailAsync("ada@example.test", displayName, Link));

        var mail = Assert.Single(sink.Rendered);
        return (mail.To.Email, mail.Subject, mail.PlainText, mail.Html);
    }

    [Fact]
    public async Task SendsOneMessageToTheRecipientWithTheUnchangedSubject()
    {
        var sent = await SendAsync();

        Assert.Equal("ada@example.test", sent.To);
        Assert.Equal("Confirm your email address", sent.Subject);
    }

    [Fact]
    public async Task HasBothAPlainTextAndAnHtmlView()
    {
        var sent = await SendAsync();

        Assert.False(string.IsNullOrWhiteSpace(sent.Plain));
        Assert.False(string.IsNullOrWhiteSpace(sent.Html));
    }

    [Fact]
    public async Task PlainTextKeepsTheLinkTheExpiryAndTheIgnoreSentence()
    {
        var sent = await SendAsync();

        Assert.Contains(Link, sent.Plain);
        Assert.Contains("This link expires in 24 hours.", sent.Plain);
        Assert.Contains("If you did not request this account, you can ignore this message.", sent.Plain);
        Assert.Contains("Hi Ada,", sent.Plain);
        Assert.DoesNotContain("<", sent.Plain);
    }

    [Fact]
    public async Task HtmlKeepsTheLinkTheExpiryAndTheIgnoreSentence()
    {
        var sent = await SendAsync();

        Assert.Contains($"href=\"{System.Net.WebUtility.HtmlEncode(Link)}\"", sent.Html);
        Assert.Contains("This link expires in 24 hours.", sent.Html);
        Assert.Contains("If you did not request this account, you can ignore this message.", sent.Html);
    }

    [Fact]
    public async Task HtmlShowsTheLogoWithSizeAndAltTextFromTheSiteItself()
    {
        var sent = await SendAsync();

        var img = Regex.Match(sent.Html, "<img\\b[^>]*>", RegexOptions.IgnoreCase);
        Assert.True(img.Success);
        Assert.Contains("src=\"https://gustavo.example/email/gcv-logo.png\"", img.Value);
        Assert.Matches("width=\"\\d+\"", img.Value);
        Assert.Matches("alt=\"[^\"]{5,}\"", img.Value);
    }

    [Fact]
    public async Task LogoSizeKeepsTheRealImageAspectRatio()
    {
        string? path = null;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "GooseWebsite.Client", "public", "email", "gcv-logo.png");
            if (File.Exists(candidate))
            {
                path = candidate;
                break;
            }
        }

        Assert.NotNull(path);
        var header = File.ReadAllBytes(path).Take(24).ToArray();
        var realWidth = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        var realHeight = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];

        var sent = await SendAsync();
        var img = Regex.Match(sent.Html, "<img\\b[^>]*>", RegexOptions.IgnoreCase).Value;
        var width = int.Parse(Regex.Match(img, "width=\"(\\d+)\"").Groups[1].Value);
        var height = int.Parse(Regex.Match(img, "height=\"(\\d+)\"").Groups[1].Value);

        Assert.InRange(Math.Abs(height - width * (double)realHeight / realWidth), 0, 1);
    }

    [Fact]
    public async Task LogoAddressIsBuiltFromThePublicBaseUrl()
    {
        var sink = new TestMailService();
        var sender = new AccountEmailSender(sink, Links("https://other.example"));

        await sender.SendVerificationEmailAsync("ada@example.test", "Ada", Link);

        Assert.Contains("src=\"https://other.example/email/gcv-logo.png\"", sink.Rendered.Single().Html);
    }

    [Fact]
    public async Task HtmlUsesNoThirdPartyOrActiveContent()
    {
        var sent = await SendAsync();

        Assert.DoesNotMatch("<script", sent.Html.ToLowerInvariant());
        Assert.DoesNotMatch("<link\\b", sent.Html.ToLowerInvariant());
        Assert.DoesNotContain("@import", sent.Html);
        Assert.DoesNotContain("url(", sent.Html);

        var images = Regex.Matches(sent.Html, "<img\\b[^>]*>", RegexOptions.IgnoreCase);
        Assert.Single(images);
        Assert.DoesNotMatch("width=\"1\"|height=\"1\"", images[0].Value);

        var references = Regex.Matches(sent.Html, "(?:src|href)=\"([^\"]*)\"", RegexOptions.IgnoreCase)
            .Select(match => System.Net.WebUtility.HtmlDecode(match.Groups[1].Value));
        foreach (var reference in references)
        {
            Assert.Equal("gustavo.example", new Uri(reference).Host);
        }
    }

    [Fact]
    public async Task DisplayNameIsHtmlEncodedInTheHtmlViewOnly()
    {
        var sent = await SendAsync("<b>x</b>");

        Assert.DoesNotContain("<b>x</b>", sent.Html);
        Assert.Contains("&lt;b&gt;x&lt;/b&gt;", sent.Html);
        Assert.Contains("Hi <b>x</b>,", sent.Plain);
    }

    [Fact]
    public async Task WhenMailIsNotConfiguredNothingIsSentAndItReportsFailure()
    {
        var sink = new TestMailService { Unconfigured = true };
        var sender = new AccountEmailSender(sink, Links());

        var result = await sender.SendVerificationEmailAsync("ada@example.test", "Ada", Link);

        Assert.False(result);
        Assert.Empty(sink.Rendered);
    }

    [Fact]
    public void PublicLinkBuilder_BuildsASiteAddressForAPath()
    {
        Assert.Equal("https://gustavo.example/email/gcv-logo.png", Links("https://gustavo.example/").SiteLink("/email/gcv-logo.png"));
        Assert.Equal("https://localhost/email/gcv-logo.png", Links(null).SiteLink("/email/gcv-logo.png"));
    }
}
