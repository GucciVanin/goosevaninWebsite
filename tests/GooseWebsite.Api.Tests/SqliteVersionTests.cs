using Microsoft.Data.Sqlite;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Guards the bundled SQLite engine against the known vulnerability (CVE-2025-6965, fixed in SQLite 3.50.2).
public sealed class SqliteVersionTests
{
    [Fact]
    public async Task BundledSqliteEngine_IsNewEnoughToIncludeTheCve20256965Fix()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version()";

        var version = Version.Parse((string)(await command.ExecuteScalarAsync())!);

        Assert.True(version >= new Version(3, 50, 2), $"SQLite {version} is older than 3.50.2.");
    }
}
