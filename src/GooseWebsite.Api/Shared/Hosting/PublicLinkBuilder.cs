namespace GooseWebsite.Api.Shared.Hosting;

/// <summary>
/// Builds the links the application emails to people. The values travel in the URL fragment, so they are
/// never sent to the server in the first request (and never reach access logs or the Referer header).
/// </summary>
public sealed class PublicLinkBuilder(IConfiguration configuration)
{
    private const string DefaultBaseUrl = "https://localhost";

    /// <summary>Builds <c>{PublicBaseUrl}{path}#key=value&amp;key=value</c> with every value escaped.</summary>
    public string FragmentLink(string path, params (string Key, string Value)[] values) =>
        $"{BaseUrl}{path}#{string.Join('&', values.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}"))}";

    private string BaseUrl => configuration["PublicBaseUrl"]?.TrimEnd('/') ?? DefaultBaseUrl;
}
