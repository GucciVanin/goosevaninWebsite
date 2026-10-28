using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Accounts.Services;
using GooseWebsite.Api.Modules.Contact.Services;
using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Provides an isolated HTTPS identity host so cookie and antiforgery behavior can be tested.
internal sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string _dataProtectionPath = Path.Combine(Path.GetTempPath(), $"goosewebsite-auth-{Guid.NewGuid():N}");
    private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), $"goosewebsite-media-{Guid.NewGuid():N}");

    /// <summary>The folder this factory's application stores uploaded images in.</summary>
    public string MediaPath => _mediaPath;
    private readonly TimeSpan? _cookieLifetime;
    private readonly TimeSpan? _confirmationTokenLifetime;
    private readonly bool _readerRegistrationEnabled;
    private readonly ICurrentCaller? _callerOverride;
    private readonly bool _includeProbeControllers;

    /// <summary>Every log message the host writes, for tests that assert what is (not) logged.</summary>
    public CapturingLoggerProvider Logs { get; } = new();

    public AuthApiFactory(
        TimeSpan? cookieLifetime = null,
        TimeSpan? confirmationTokenLifetime = null,
        bool readerRegistrationEnabled = true,
        ICurrentCaller? callerOverride = null,
        bool includeProbeControllers = false)
    {
        _cookieLifetime = cookieLifetime;
        _confirmationTokenLifetime = confirmationTokenLifetime;
        _readerRegistrationEnabled = readerRegistrationEnabled;
        _callerOverride = callerOverride;
        _includeProbeControllers = includeProbeControllers;
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("DataProtection:KeysPath", _dataProtectionPath);
        builder.UseSetting("Media:Directory", _mediaPath);
        builder.UseSetting("Auth:ReaderRegistration:Enabled", _readerRegistrationEnabled.ToString());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            services.AddSingleton<ILoggerProvider>(Logs);
            if (_callerOverride is not null)
            {
                services.RemoveAll<ICurrentCaller>();
                services.AddSingleton(_callerOverride);
            }

            if (_includeProbeControllers)
            {
                // Adds the probe controllers of this test project (UnitOfWorkProbeController) to the host.
                services.AddControllersWithViews().AddApplicationPart(typeof(AuthApiFactory).Assembly);
            }

            services.RemoveAll<IContactEmailDeliveryService>();
            services.AddSingleton<StubContactEmailDeliveryService>();
            services.AddSingleton<IContactEmailDeliveryService>(provider => provider.GetRequiredService<StubContactEmailDeliveryService>());
            services.RemoveAll<IMailService>();
            services.AddSingleton<TestMailService>();
            services.AddSingleton<IMailService>(provider => provider.GetRequiredService<TestMailService>());
            if (_cookieLifetime is not null)
            {
                services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
                {
                    options.ExpireTimeSpan = _cookieLifetime.Value;
                    options.SlidingExpiration = false;
                });
            }

            if (_confirmationTokenLifetime is not null)
            {
                services.PostConfigure<DataProtectionTokenProviderOptions>(options =>
                    options.TokenLifespan = _confirmationTokenLifetime.Value);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            if (Directory.Exists(_mediaPath))
            {
                Directory.Delete(_mediaPath, recursive: true);
            }

            if (Directory.Exists(_dataProtectionPath))
            {
                Directory.Delete(_dataProtectionPath, recursive: true);
            }
        }
    }

    public async Task ProvisionAdministratorAsync(string email = "admin@example.test", string password = "Strong-password-123")
    {
        using var scope = Services.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<AdminProvisioningService>();
        var result = await provisioner.ProvisionAsync(email, password);
        Assert.True(result.Succeeded, result.ErrorMessage);
    }

    /// <summary>The one mail double: records every email the application would send.</summary>
    public TestMailService Mail => Services.GetRequiredService<TestMailService>();

    public StubContactEmailDeliveryService ContactEmail => Services.GetRequiredService<StubContactEmailDeliveryService>();

    public async Task ProvisionStandardAccountAsync(string email = "reader@example.test", string password = "Strong-password-123")
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Reader"))
        {
            Assert.True((await roles.CreateAsync(new IdentityRole("Reader"))).Succeeded);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = "Test reader",
            EmailConfirmed = true
        };
        var create = await users.CreateAsync(user, password);
        Assert.True(create.Succeeded, string.Join(", ", create.Errors.Select(error => error.Code)));
        Assert.True((await users.AddToRoleAsync(user, "Reader")).Succeeded);
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true
    });
}