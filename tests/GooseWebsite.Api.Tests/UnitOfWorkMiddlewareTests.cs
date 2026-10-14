using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// Saves made outside an MVC action (middleware, minimal endpoints) belong to the same one-click unit of work,
// and a client that disconnects must not undo a click the server already finished.
public sealed class UnitOfWorkMiddlewareTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _services;

    public UnitOfWorkMiddlewareTests()
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentCaller>(new TestCaller(CallerKind.Administrator) { InHttpRequest = true });
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        _services = services.BuildServiceProvider();

        using var seedContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        seedContext.Database.EnsureCreated();
        seedContext.Users.Add(new ApplicationUser { Id = "u1", UserName = "u", NormalizedUserName = "U", DisplayName = "U" });
        seedContext.SaveChanges();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<bool> PostExistsAsync(string slug)
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        return await context.Set<BlogPost>().AnyAsync(post => post.Slug == slug);
    }

    private HttpContext NewRequest(IServiceScope scope, CancellationToken aborted = default) =>
        new DefaultHttpContext { RequestServices = scope.ServiceProvider, RequestAborted = aborted };

    private static async Task SaveAPostAsync(HttpContext http, string slug)
    {
        var context = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        context.Set<BlogPost>().Add(new BlogPost { Title = slug, Slug = slug, AuthorId = "u1" });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ASaveMadeOutsideAnActionIsCommittedWhenTheRequestFinishesNormally()
    {
        using var scope = _services.CreateScope();
        var http = NewRequest(scope);
        var middleware = new UnitOfWorkMiddleware(async context => await SaveAPostAsync(context, "from-middleware"));

        await middleware.InvokeAsync(http);

        Assert.True(await PostExistsAsync("from-middleware"));
    }

    [Fact]
    public async Task ASaveMadeOutsideAnActionIsRolledBackWhenTheRequestThrows()
    {
        using var scope = _services.CreateScope();
        var http = NewRequest(scope);
        var middleware = new UnitOfWorkMiddleware(async context =>
        {
            await SaveAPostAsync(context, "then-failed");
            throw new InvalidOperationException("the click failed");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(http));

        Assert.False(await PostExistsAsync("then-failed"));
    }

    [Fact]
    public async Task AClientThatDisconnectsAfterTheServerFinishedDoesNotUndoTheClick()
    {
        using var scope = _services.CreateScope();
        using var disconnected = new CancellationTokenSource();
        var http = NewRequest(scope, disconnected.Token);
        var middleware = new UnitOfWorkMiddleware(async context =>
        {
            await SaveAPostAsync(context, "saved-before-disconnect");
            await disconnected.CancelAsync();
        });

        await middleware.InvokeAsync(http);

        Assert.True(await PostExistsAsync("saved-before-disconnect"));
    }
}
