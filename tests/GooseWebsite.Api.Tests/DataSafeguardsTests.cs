using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F7-US1: what each kind of caller may read and write is decided in one place, whatever the module asks for.
public sealed class DataSafeguardsTests
{
    private static async Task<(AuthApiFactory Factory, TestCaller Caller)> CreateHostWithPostsAsync()
    {
        var caller = new TestCaller(CallerKind.System);
        var factory = new AuthApiFactory(callerOverride: caller);
        await factory.ProvisionAdministratorAsync();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var admin = await database.FindUserByEmailAsync("admin@example.test");
        database.Add(new BlogPost { Title = "Published", Slug = "published", IsPublished = true, AuthorId = admin!.Id });
        database.Add(new BlogPost { Title = "Draft", Slug = "draft", IsPublished = false, AuthorId = admin.Id });
        await database.SaveChangesAsync();
        return (factory, caller);
    }

    private static async Task<List<string>> SlugsSeenAsync(AuthApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        return await database.Read<BlogPost>().OrderBy(post => post.Slug).Select(post => post.Slug).ToListAsync();
    }

    [Theory]
    [InlineData(CallerKind.Anonymous)]
    [InlineData(CallerKind.Authenticated)]
    public async Task Read_HidesDraftsFromEveryoneWhoIsNotAnAdministrator(CallerKind kind)
    {
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        caller.Kind = kind;

        Assert.Equal(["published"], await SlugsSeenAsync(factory));
    }

    [Theory]
    [InlineData(CallerKind.Administrator)]
    [InlineData(CallerKind.System)]
    public async Task Read_ShowsDraftsToAdministratorsAndTheSystem(CallerKind kind)
    {
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        caller.Kind = kind;

        Assert.Equal(["draft", "published"], await SlugsSeenAsync(factory));
    }

    [Theory]
    [InlineData(CallerKind.Anonymous)]
    [InlineData(CallerKind.Authenticated)]
    public async Task Write_IsRefusedForEveryoneWhoIsNotAnAdministrator_AndNothingChanges(CallerKind kind)
    {
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        caller.Kind = kind;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var authorId = (await database.Read<ApplicationUser>().FirstAsync()).Id;

        database.Add(new BlogPost { Title = "Sneaky", Slug = "sneaky", IsPublished = true, AuthorId = authorId });
        var refused = await Assert.ThrowsAsync<DataAccessDeniedException>(() => database.SaveChangesAsync());

        Assert.Equal(nameof(BlogPost), refused.EntityName);
        caller.Kind = CallerKind.System;
        Assert.Equal(["draft", "published"], await SlugsSeenAsync(factory));
    }

    [Fact]
    public async Task Write_ModifyingOrDeletingAnExistingPostIsRefusedForANonAdministrator()
    {
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var published = await database.ReadForUpdate<BlogPost>().SingleAsync(post => post.Slug == "published");

        caller.Kind = CallerKind.Anonymous;
        published.Title = "Defaced";
        await Assert.ThrowsAsync<DataAccessDeniedException>(() => database.SaveChangesAsync());
        database.Remove(published);
        await Assert.ThrowsAsync<DataAccessDeniedException>(() => database.SaveChangesAsync());

        using var checkScope = factory.Services.CreateScope();
        caller.Kind = CallerKind.System;
        var stored = await checkScope.ServiceProvider.GetRequiredService<DatabaseService>()
            .Read<BlogPost>().SingleAsync(post => post.Slug == "published");
        Assert.Equal("Published", stored.Title);
    }

    [Theory]
    [InlineData(CallerKind.Administrator)]
    [InlineData(CallerKind.System)]
    public async Task Write_IsAllowedForAdministratorsAndTheSystem(CallerKind kind)
    {
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        caller.Kind = kind;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var authorId = (await database.Read<ApplicationUser>().FirstAsync()).Id;

        database.Add(new BlogPost { Title = "Allowed", Slug = "allowed", IsPublished = true, AuthorId = authorId });
        await database.SaveChangesAsync();

        Assert.Contains("allowed", await SlugsSeenAsync(factory));
    }

    [Fact]
    public async Task Identity_AnonymousVisitorsCanStillRegisterAndSignIn()
    {
        // Identity tables are system-managed: registration and the sign-in lockout counter write them for anonymous callers.
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        caller.Kind = CallerKind.Anonymous;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();

        var created = await database.CreateUserAsync(new ApplicationUser { UserName = "visitor", Email = "visitor@example.test", DisplayName = "Visitor" }, "Strong-password-123");

        Assert.True(created.Succeeded);
    }

    [Fact]
    public async Task ReadFilter_FollowsTheCallerOfEachContext_NotTheFirstContextThatBuiltTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using (var seed = new ApplicationDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Users.Add(new ApplicationUser { Id = "u1", UserName = "u", NormalizedUserName = "U", DisplayName = "U" });
            seed.Set<BlogPost>().Add(new BlogPost { Title = "P", Slug = "p", IsPublished = true, AuthorId = "u1" });
            seed.Set<BlogPost>().Add(new BlogPost { Title = "D", Slug = "d", IsPublished = false, AuthorId = "u1" });
            await seed.SaveChangesAsync();
        }

        await using (var administrator = new ApplicationDbContext(options, new TestCaller(CallerKind.Administrator)))
        {
            Assert.Equal(2, await administrator.Set<BlogPost>().CountAsync());
        }

        await using (var anonymous = new ApplicationDbContext(options, new TestCaller(CallerKind.Anonymous)))
        {
            Assert.Equal(1, await anonymous.Set<BlogPost>().CountAsync());
        }

        await using (var administratorAgain = new ApplicationDbContext(options, new TestCaller(CallerKind.Administrator)))
        {
            Assert.Equal(2, await administratorAgain.Set<BlogPost>().CountAsync());
        }
    }

    [Fact]
    public async Task Save_TrimsTextAndStripsControlCharactersButKeepsLineBreaksAndTabs()
    {
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        caller.Kind = CallerKind.Administrator;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var authorId = (await database.Read<ApplicationUser>().FirstAsync()).Id;

        database.Add(new BlogPost
        {
            Title = "  Hello\u0007 \u0000world  ",
            Slug = "sanitized",
            Excerpt = "\u001b[31mred\u001b[0m",
            Content = "  first line\nsecond\tcolumn\r\nthird  ",
            Category = "​Notes\u0008",
            IsPublished = true,
            AuthorId = authorId
        });
        await database.SaveChangesAsync();

        caller.Kind = CallerKind.System;
        using var readScope = factory.Services.CreateScope();
        var stored = await readScope.ServiceProvider.GetRequiredService<DatabaseService>()
            .Read<BlogPost>().SingleAsync(post => post.Slug == "sanitized");
        Assert.Equal("Hello world", stored.Title);
        Assert.Equal("[31mred[0m", stored.Excerpt);
        Assert.Equal("first line\nsecond\tcolumn\r\nthird", stored.Content);
        Assert.DoesNotContain('\u0008', stored.Category);
    }

    [Fact]
    public async Task Save_RejectsATextOverItsDeclaredLimit_AndStoresNothing()
    {
        var (factory, _) = await CreateHostWithPostsAsync();
        using var _factory = factory;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();

        var rejected = await Assert.ThrowsAsync<DataRejectedException>(() => database.CreateUserAsync(
            new ApplicationUser { UserName = "long-name", Email = "long@example.test", DisplayName = new string('x', 101) },
            "Strong-password-123"));

        Assert.Equal(nameof(ApplicationUser), rejected.EntityName);
        Assert.Equal(nameof(ApplicationUser.DisplayName), rejected.PropertyName);
        Assert.Null(await database.FindUserByEmailAsync("long@example.test"));
    }

    [Fact]
    public async Task Save_BlogPostsHaveNoLengthLimit()
    {
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        caller.Kind = CallerKind.Administrator;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var authorId = (await database.Read<ApplicationUser>().FirstAsync()).Id;

        database.Add(new BlogPost { Title = new string('t', 5000), Slug = "long", Content = new string('c', 300_000), IsPublished = true, AuthorId = authorId });
        await database.SaveChangesAsync();

        Assert.Contains("long", await SlugsSeenAsync(factory));
    }

    [Fact]
    public async Task Logs_NameTheKindOfRejectionAndTheEntityButNeverTheRejectedValues()
    {
        var (factory, caller) = await CreateHostWithPostsAsync();
        using var _ = factory;
        caller.Kind = CallerKind.Anonymous;
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var authorId = (await database.Read<ApplicationUser>().FirstAsync()).Id;
        database.Add(new BlogPost { Title = "SECRET-TITLE-123", Slug = "secret", Content = "SECRET-BODY-456", AuthorId = authorId });

        await Assert.ThrowsAsync<DataAccessDeniedException>(() => database.SaveChangesAsync());

        var logs = string.Join('\n', factory.Logs.Messages);
        Assert.Contains(nameof(BlogPost), logs);
        Assert.DoesNotContain("SECRET-TITLE-123", logs);
        Assert.DoesNotContain("SECRET-BODY-456", logs);
    }
}
