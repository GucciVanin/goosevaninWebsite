using GooseWebsite.Api.Modules.Contact.Services;
using GooseWebsite.Api.Shared.Email;
using Xunit;

namespace GooseWebsite.Api.Tests;

// TE11 review follow-ups: mailto encoding, surrogate-safe truncation, and the pinned unsubscribe footer.
public sealed class ContactRenderedMailFollowUpTests
{
    private static readonly MailAddressee Owner = new("gustavo@example.test", "Gustavo");

    [Fact]
    public void OwnerNotice_PercentEncodesTheSenderAddressInTheMailtoLink()
    {
        var mail = ContactEmailRenderer.OwnerNotice(Owner, "Eve", "a?bcc=x&body=y@attacker.example", "Personal note", "hi", "https://site.test/logo.png", "https://site.test");

        Assert.Contains("href=\"mailto:a%3Fbcc%3Dx%26body%3Dy%40attacker.example\"", mail.Html);
        foreach (System.Text.RegularExpressions.Match href in System.Text.RegularExpressions.Regex.Matches(mail.Html, "href=\"(mailto:[^\"]*)\""))
        {
            Assert.DoesNotContain("?", href.Groups[1].Value);
            Assert.DoesNotContain("&", href.Groups[1].Value);
        }
    }

    [Fact]
    public void OwnerNotice_TruncatesALongNameWithoutSplittingASurrogatePair()
    {
        var name = new string('a', 59) + "\U0001F600" + "tail";

        var mail = ContactEmailRenderer.OwnerNotice(Owner, name, "eve@example.test", "Personal note", "hi", "https://site.test/logo.png", "https://site.test");

        // The button label is cut at 60 characters; the emoji straddles the cut, so it is dropped whole.
        Assert.Contains("Reply to " + new string('a', 59) + " &rarr;", mail.Html);

        for (var i = 0; i < mail.Html.Length; i++)
        {
            if (char.IsHighSurrogate(mail.Html[i]))
            {
                Assert.True(i + 1 < mail.Html.Length && char.IsLowSurrogate(mail.Html[i + 1]), "lone high surrogate");
                i++;
            }
            else
            {
                Assert.False(char.IsLowSurrogate(mail.Html[i]), "lone low surrogate");
            }
        }
    }

    [Fact]
    public void ContactMails_HaveNoUnsubscribeFooter()
    {
        var mail = ContactEmailRenderer.Confirmation(new MailAddressee("eve@example.test", "Eve"), "https://site.test/contact/verify#token=t", "https://site.test/logo.png", "https://site.test");

        Assert.DoesNotContain("nsubscribe", mail.Html);
        Assert.Contains("You are receiving this because", mail.Html);
    }
}
