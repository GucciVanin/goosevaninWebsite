using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using VaninWebsite.Api.Modules.Accounts.Domain;
using VaninWebsite.Api.Modules.Accounts.Services;
using VaninWebsite.Api.Shared.Persistence;
using Xunit;

namespace VaninWebsite.Api.Tests;

// Provides an isolated HTTPS identity host so cookie and antiforgery behavior can be tested.
internal sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string _dataProtectionPath = Path.Combine(Path.GetTempPath(), $"vaninwebsite-auth-{Guid.NewGuid():N}");
    private readonly TimeSpan? _cookieLifetime;
    private readonly TimeSpan? _confirmationTokenLifetime;
    private readonly bool _readerRegistrationEnabled;

    public AuthApiFactory(
        TimeSpan? cookieLifetime = null,
        TimeSpan? confirmationTokenLifetime = null,
        bool readerRegistrationEnabled = true)
    {
        _cookieLifetime = cookieLifetime;
        _confirmationTokenLifetime = confirmationTokenLifetime;
        _readerRegistrationEnabled = readerRegistrationEnabled;
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("DataProtection:KeysPath", _dataProtectionPath);
        builder.UseSetting("Auth:ReaderRegistration:Enabled", _readerRegistrationEnabled.ToString());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            services.RemoveAll<IAccountEmailSender>();
            services.AddSingleton<TestAccountEmailSender>();
            services.AddSingleton<IAccountEmailSender>(provider => provider.GetRequiredService<TestAccountEmailSender>());
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

    public TestAccountEmailSender AccountEmailSender => Services.GetRequiredService<TestAccountEmailSender>();

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