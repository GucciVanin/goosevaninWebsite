using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace GooseWebsite.Api.Shared.Hosting;

public static class ForwardedHeadersServiceCollectionExtensions
{
    /// <summary>
    /// Trusts <c>X-Forwarded-For</c>/<c>X-Forwarded-Proto</c> only from the proxies listed under
    /// <c>ForwardedHeaders:KnownProxies</c>; caller-supplied headers are otherwise ignored.
    /// </summary>
    public static IServiceCollection AddTrustedProxyHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var proxy in knownProxies)
            {
                if (!IPAddress.TryParse(proxy, out var address))
                {
                    throw new InvalidOperationException(
                        $"Configured forwarded-header proxy address '{proxy}' is not a valid IP address.");
                }

                options.KnownProxies.Add(address);
            }
        });
        return services;
    }
}
