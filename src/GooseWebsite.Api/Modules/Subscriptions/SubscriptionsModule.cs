using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using GooseWebsite.Api.Modules.Subscriptions.Models;
using GooseWebsite.Api.Modules.Subscriptions.Services;

namespace GooseWebsite.Api.Modules.Subscriptions;

/// <summary>
/// Subscriptions module (F10): visitors subscribe with a name and an email, confirm through an emailed link
/// (double opt-in), and are later told about new stories and projects. The table mapping and the access rule are
/// picked up automatically (<c>SubscriberConfiguration</c>, <c>SubscriberAccessRule</c>).
/// </summary>
public static class SubscriptionsModule
{
    public const string SubscribeRateLimitPolicy = "subscriptions-subscribe";
    public const string ConfirmRateLimitPolicy = "subscriptions-confirm";

    public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SubscriptionOptions>(configuration.GetSection(SubscriptionOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<ISubscriptionLinks, SubscriptionLinks>();
        services.AddTransient<ISubscriptionEmailSender, SubscriptionEmailSender>();
        services.AddHostedService<PendingSubscriberCleanupService>();
        AddRateLimiting(services);
        return services;
    }

    // Per client address, like the other public forms. The limits come from SubscriptionOptions when the first
    // request of a client arrives.
    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(SubscribeRateLimitPolicy, context => PartitionByClient(context, SubscribeRateLimitPolicy, limits => limits.PermitLimit));
            limiter.AddPolicy(ConfirmRateLimitPolicy, context => PartitionByClient(context, ConfirmRateLimitPolicy, limits => limits.ConfirmPermitLimit));
        });
    }

    private static RateLimitPartition<string> PartitionByClient(HttpContext context, string policy, Func<SubscriptionRateLimitOptions, int> permits)
    {
        var limits = context.RequestServices.GetRequiredService<IOptions<SubscriptionOptions>>().Value.RateLimit;
        return RateLimitPartition.GetFixedWindowLimiter(
            $"{policy}:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, permits(limits)),
                Window = TimeSpan.FromMinutes(Math.Max(1, limits.WindowMinutes)),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }
}
