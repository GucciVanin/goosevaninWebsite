using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Guards table and index names, because the schema is created by EnsureCreated and existing databases cannot be altered.
public sealed class SchemaCompatibilityTests
{
    [Fact]
    public async Task BlogPosts_KeepTheirHistoricalTableNameAndUniqueSlugIndex()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var entity = context.Model.FindEntityType(typeof(BlogPost));
        Assert.NotNull(entity);
        Assert.Equal("BlogPosts", entity!.GetTableName());
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(BlogPost.Slug));

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'BlogPosts'";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
    }
}
