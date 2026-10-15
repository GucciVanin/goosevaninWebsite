using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
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
            // A database made before migrations existed has the schema of the first migration and no migration history.
            // Build exactly that (apply InitialCreate only, then forget the history) and give it a published story.
            var legacy = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await legacy.Database.GetService<IMigrator>().MigrateAsync("20261012182118_InitialCreate");
            await legacy.Database.ExecuteSqlRawAsync("DROP TABLE \"__EFMigrationsHistory\"");
            await legacy.Database.ExecuteSqlRawAsync(
                "INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, DisplayName, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) " +
                "VALUES ('u1', 'a@example.test', 'A@EXAMPLE.TEST', 'Existing', 0, 0, 0, 0, 0)");
            await legacy.Database.ExecuteSqlRawAsync(
                "INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, Category, Tags, IsPublished, CreatedAtUtc, UpdatedAtUtc, AuthorId) " +
                "VALUES ('Kept', 'kept', 'e', 'c', 'Notes', '', 1, '2026-01-01 00:00:00', '2026-01-02 00:00:00', 'u1')");
        }

        await new DatabaseInitializer().InitializeAsync(provider);

        // Later migrations then run on top, and a story that was already published gets a publication date.
        Assert.Equal(1L, await CountAsync(connection, "SELECT COUNT(*) FROM BlogPosts WHERE Slug = 'kept' AND PublishedAtUtc = CreatedAtUtc"));
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
