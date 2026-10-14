using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Shared.Hosting;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// The container reports healthy only when the application and its database both answer (TE10).
public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Health_WhenTheDatabaseAnswers_ReportsOkWithTheStartTime()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("ok", body.RootElement.GetProperty("database").GetString());
        Assert.True(body.RootElement.GetProperty("startedAtUtc").GetDateTime() <= DateTime.UtcNow);
    }

    [Fact]
    public async Task Health_IsPublicAndNeedsNoSession()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/health");

        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WhenTheDatabaseCannotBeReached_ReportsUnhealthyWithoutDetails()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=/this-folder-does-not-exist/goosewebsite.db;Mode=ReadOnly")
            .Options;
        await using var context = new ApplicationDbContext(options);

        var result = await HealthEndpoint.CheckAsync(context, CancellationToken.None);

        var json = Assert.IsType<JsonHttpResult<HealthEndpoint.HealthReport>>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, json.StatusCode);
        Assert.Equal("unhealthy", json.Value!.Status);
        Assert.Equal("unavailable", json.Value.Database);
    }
}
