using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Protects startup schema creation (TE1): new databases are built by migrations, databases created before
// migrations existed are adopted without touching their data, and the model never drifts from the migrations.
public sealed class DatabaseInitializerTests
{
    private static async Task<ServiceProvider> CreateProviderAsync(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        return services.BuildServiceProvider();
    }

    private static async Task<long> CountAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task Initialize_OnANewDatabase_BuildsTheSchemaFromMigrations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await using var provider = await CreateProviderAsync(connection);

        await new DatabaseInitializer().InitializeAsync(provider);

        Assert.Equal(1L, await CountAsync(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'BlogPosts'"));
        Assert.Equal(1L, await CountAsync(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'AspNetUsers'"));
        Assert.True(await CountAsync(connection, "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"") >= 1);
    }

    [Fact]
    public async Task Initialize_OnADatabaseCreatedBeforeMigrations_KeepsItsDataAndAdoptsItIntoMigrations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await using var provider = await CreateProviderAsync(connection);
        await using (var scope = provider.CreateAsyncScope())
        {
            var legacy = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await legacy.Database.EnsureCreatedAsync();
            legacy.Users.Add(new ApplicationUser { Id = "u1", UserName = "a@example.test", NormalizedUserName = "A@EXAMPLE.TEST", DisplayName = "Existing" });
            legacy.Set<BlogPost>().Add(new BlogPost { Title = "Kept", Slug = "kept", AuthorId = "u1" });
            await legacy.SaveChangesAsync();
        }

        await new DatabaseInitializer().InitializeAsync(provider);

        Assert.Equal(1L, await CountAsync(connection, "SELECT COUNT(*) FROM BlogPosts WHERE Slug = 'kept'"));
        Assert.Equal(1L, await CountAsync(connection, "SELECT COUNT(*) FROM AspNetUsers WHERE Id = 'u1'"));
        Assert.True(await CountAsync(connection, "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"") >= 1);
    }

    [Fact]
    public async Task Initialize_OnAPartialLegacyDatabase_RefusesToAdoptItAndChangesNothing()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await using var provider = await CreateProviderAsync(connection);
        await using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE AspNetUsers (Id TEXT PRIMARY KEY)";
            await setup.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => new DatabaseInitializer().InitializeAsync(provider));

        Assert.Equal(0L, await CountAsync(connection, "SELECT COUNT(*) FROM sqlite_master WHERE name = '__EFMigrationsHistory'"));
    }

    [Fact]
    public async Task Initialize_RunTwice_ChangesNothingTheSecondTime()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await using var provider = await CreateProviderAsync(connection);
        await new DatabaseInitializer().InitializeAsync(provider);
        var historyAfterFirstRun = await CountAsync(connection, "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"");

        await new DatabaseInitializer().InitializeAsync(provider);

        Assert.Equal(historyAfterFirstRun, await CountAsync(connection, "SELECT COUNT(*) FROM \"__EFMigrationsHistory\""));
    }

    [Fact]
    public async Task Migrations_MatchTheCurrentModel_SoNoSchemaChangeIsForgotten()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await using var provider = await CreateProviderAsync(connection);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "The model changed without a migration. Add one with `dotnet ef migrations add <Name>`.");
    }
}
