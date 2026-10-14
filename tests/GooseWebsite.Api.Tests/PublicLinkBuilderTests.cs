using Microsoft.Extensions.Configuration;
using GooseWebsite.Api.Shared.Hosting;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Protects the one place that builds the links emailed to people (Contact and Accounts share it).
public sealed class PublicLinkBuilderTests
{
    private static PublicLinkBuilder Create(string? publicBaseUrl)
    {
        var settings = new Dictionary<string, string?>();
        if (publicBaseUrl is not null)
        {
            settings["PublicBaseUrl"] = publicBaseUrl;
        }

        return new PublicLinkBuilder(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    [Fact]
    public void FragmentLink_PutsTheValuesInTheFragmentSoTheyNeverReachTheServerInTheFirstRequest()
    {
        var link = Create("https://gustavo.example/").FragmentLink("/verify-email", ("userId", "a b"), ("token", "x/y+z="));

        Assert.Equal("https://gustavo.example/verify-email#userId=a%20b&token=x%2Fy%2Bz%3D", link);
        Assert.Empty(new Uri(link).Query);
    }

    [Fact]
    public void FragmentLink_FallsBackToLocalhostWhenNoPublicAddressIsConfigured()
    {
        var link = Create(null).FragmentLink("/contact/verify", ("token", "abc"));

        Assert.Equal("https://localhost/contact/verify#token=abc", link);
    }
}
