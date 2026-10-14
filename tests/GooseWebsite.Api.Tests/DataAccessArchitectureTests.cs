using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F7-US1 acceptance test 4 and Gustavo's decision that every entity declares its own access rule.
public sealed class DataAccessArchitectureTests
{
    [Fact]
    public async Task EveryEntityInTheModelDeclaresAnAccessRule()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

        var withoutRule = context.Model.GetEntityTypes()
            .Select(entity => entity.ClrType)
            .Where(type => AccessRuleCatalog.RuleFor(type) is null)
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            withoutRule.Count == 0,
            $"These entities declare no access rule (add an EntityAccessRule<T> next to their configuration): {string.Join(", ", withoutRule)}");
    }

    [Fact]
    public void NoModuleTouchesTheDatabaseContextOrTheIdentityStoresDirectly()
    {
        var apiRoot = Path.Combine(FindRepositoryRoot(), "src", "GooseWebsite.Api");
        var forbidden = new[] { "ApplicationDbContext", "UserManager<", "RoleManager<", "IgnoreQueryFilters", "ExecuteUpdate", "ExecuteDelete", "FromSql", "ExecuteSql" };
        var allowedPersistence = Path.Combine(apiRoot, "Shared", "Persistence") + Path.DirectorySeparatorChar;
        var migrations = Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar;
        // Registering Identity's EF store is the one allowed mention outside the persistence module.
        var identityRegistration = Path.Combine(apiRoot, "Modules", "Accounts", "AccountsModule.cs");

        var offenders = Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                && !file.Contains(migrations)
                && !file.StartsWith(allowedPersistence, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(file, identityRegistration, StringComparison.OrdinalIgnoreCase))
            .SelectMany(file => forbidden.Where(File.ReadAllText(file).Contains).Select(name => $"{Path.GetRelativePath(apiRoot, file)} mentions {name}"))
            .ToList();

        Assert.True(offenders.Count == 0, "Go through DatabaseService instead:\n" + string.Join('\n', offenders));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GooseWebsite.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("GooseWebsite.slnx was not found above the test output.");
    }
}
