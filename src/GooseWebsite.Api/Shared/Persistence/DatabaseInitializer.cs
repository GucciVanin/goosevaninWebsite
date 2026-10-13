using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GooseWebsite.Api.Shared.Modules;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// Brings the schema up to date with EF Core migrations before any module initializer runs.
/// </summary>
/// <remarks>
/// A database created before migrations existed (by <c>EnsureCreated</c>) already holds the tables of the
/// first migration but has no migration history. It is adopted by recording that migration as applied,
/// so its data is untouched. Add schema changes with <c>dotnet ef migrations add</c>; a test fails when
/// the model and the migrations disagree.
/// </remarks>
public sealed class DatabaseInitializer : IModuleInitializer
{
    private const string LegacyMarkerTable = "AspNetUsers";

    // Tables of the first migration that a database made by EnsureCreated must already hold.
    private static readonly string[] BaselineTables = ["AspNetUsers", "AspNetRoles", "AspNetUserRoles", "BlogPosts"];

    public async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        if (await IsPreMigrationDatabaseAsync(context, cancellationToken))
        {
            await BaselineAsync(context, cancellationToken);
        }

        await context.Database.MigrateAsync(cancellationToken);
    }

    private static async Task<bool> IsPreMigrationDatabaseAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            return false;
        }

        var applied = await context.Database.GetService<IHistoryRepository>().ExistsAsync(cancellationToken)
            ? await context.Database.GetAppliedMigrationsAsync(cancellationToken)
            : [];
        if (applied.Any())
        {
            return false;
        }

        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var present = new List<string>();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    present.Add(reader.GetString(0));
                }
            }

            if (!present.Contains(LegacyMarkerTable, StringComparer.Ordinal))
            {
                return false;
            }

            // Adopting a database that only partly matches would hide the gap: later queries would fail
            // and migrations would never repair it. Stop and let the operator inspect it.
            var missing = BaselineTables.Where(table => !present.Contains(table, StringComparer.Ordinal)).ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The database has no migration history and does not match the expected schema (missing tables: {string.Join(", ", missing)}). Restore it from a backup or inspect it before starting.");
            }

            return true;
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    // Marks the first migration as applied without running it, because its tables already exist.
    private static async Task BaselineAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var firstMigration = context.Database.GetService<IMigrationsAssembly>().Migrations.Keys.Order().First();
        var history = context.Database.GetService<IHistoryRepository>();

        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            history.GetInsertScript(new HistoryRow(firstMigration, ProductInfo.GetVersion())),
            cancellationToken);
    }
}
