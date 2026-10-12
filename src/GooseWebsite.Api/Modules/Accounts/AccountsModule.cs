using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using GooseWebsite.Api.Modules.Accounts.Authorization;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Accounts.Services;
using GooseWebsite.Api.Shared.Modules;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Accounts;

/// <summary>
/// Accounts module: ASP.NET Core Identity users and roles, cookie sessions, CSRF protection,
/// authorization policies, and operator-only administrator provisioning.
/// </summary>
public static class AccountsModule
{
    public const string ReaderAccountEmailRateLimitPolicy = "reader-account-email";

    public static IServiceCollection AddAccountsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var cookieSecurePolicy = environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;

        AddDataProtection(services, configuration, environment);
        AddIdentity(services);
        AddRateLimiting(services);
        AddAuthorizationPolicies(services);

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "goosewebsite.csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "goosewebsite.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;

            // API callers get status codes, never a redirect to an HTML login page.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddScoped<AdminProvisioningService>();
        services.AddTransient<IAccountEmailSender, SmtpAccountEmailSender>();
        services.AddScoped<IModuleInitializer, AccountRoleInitializer>();
        services.AddScoped<AdminPasswordResetService>();
        services.AddSingleton<IModuleCommand, AdminProvisioningCommand>();
        services.AddSingleton<IModuleCommand, AdminPasswordResetCommand>();
        return services;
    }

    private static void AddDataProtection(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Keys must persist across restarts or every session cookie becomes invalid.
        var keysPath = configuration["DataProtection:KeysPath"]
            ?? Path.Combine(environment.ContentRootPath, "data-protection-keys");
        Directory.CreateDirectory(keysPath);
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(24));

        // Check the security stamp on every request so a password reset ends open sessions at once
        // instead of after the default 30 minutes.
        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.Zero);
    }

    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(ReaderAccountEmailRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });
    }

    private static void AddAuthorizationPolicies(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(AccountRoles.Admin));
            options.AddPolicy(AuthorizationPolicies.VerifiedReader, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AccountRoles.Reader)
                .AddRequirements(new VerifiedReaderRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, VerifiedReaderAuthorizationHandler>();
    }
}
