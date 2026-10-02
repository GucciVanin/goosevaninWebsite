using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VaninWebsite.Api.Shared.Modules;

namespace VaninWebsite.Api.Shared.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string DefaultConnectionString = "Data Source=Data/vaninwebsite.db";

    /// <summary>
    /// Registers the SQLite database context and the schema initializer. Register it before the
    /// modules so the schema exists when their initializers run.
    /// </summary>
    public static IServiceCollection AddSharedPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = ResolveConnectionString(
            configuration.GetConnectionString("Default") ?? DefaultConnectionString,
            environment.ContentRootPath);

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<DatabaseService>();
        services.AddScoped<IModuleInitializer, DatabaseInitializer>();
        return services;
    }

    // Anchors a relative database path to the content root so the working directory never matters.
    private static string ResolveConnectionString(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource == ":memory:"
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(dataSource))
        {
            return connectionString;
        }

        builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, dataSource));
        Directory.CreateDirectory(Path.GetDirectoryName(builder.DataSource)!);
        return builder.ToString();
    }
}
