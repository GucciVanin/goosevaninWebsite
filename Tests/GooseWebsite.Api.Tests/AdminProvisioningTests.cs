using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Accounts.Services;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Protects the privileged account workflow from partial or unsafe provisioning.
public sealed class AdminProvisioningTests
{
    [Fact]
    public async Task ProvisionAsync_AllowsMultipleAdministratorsButDoesNotPromoteExistingAccounts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme);
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();
        services.AddScoped<DatabaseService>();
        services.AddScoped<AdminProvisioningService>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await database.Database.EnsureCreatedAsync();
        var provisioner = scope.ServiceProvider.GetRequiredService<AdminProvisioningService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var first = await provisioner.ProvisionAsync("admin1@example.com", "Secure-password1");
        var second = await provisioner.ProvisionAsync("admin2@example.com", "Secure-password1");
        var duplicate = await provisioner.ProvisionAsync("admin1@example.com", "another-password");
        var firstUser = await userManager.FindByEmailAsync("admin1@example.com");
        var secondUser = await userManager.FindByEmailAsync("admin2@example.com");

        Assert.True(first.Succeeded, first.ErrorMessage);
        Assert.True(second.Succeeded, second.ErrorMessage);
        Assert.False(duplicate.Succeeded);
        Assert.NotNull(firstUser);
        Assert.NotNull(secondUser);
        Assert.True(firstUser!.EmailConfirmed);
        Assert.True(secondUser!.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(firstUser, "Admin"));
        Assert.True(await userManager.IsInRoleAsync(secondUser, "Admin"));
        Assert.Equal(2, await database.Users.CountAsync());
        Assert.Equal("Site administrator", firstUser.DisplayName);
    }
}
