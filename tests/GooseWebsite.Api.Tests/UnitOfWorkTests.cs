using System.Net;
using Microsoft.Extensions.DependencyInjection;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F7-US1 / Gustavo's decision: one click is one unit of work. If any part of it fails, nothing from that click is kept.
public sealed class UnitOfWorkTests
{
    private static async Task<bool> UserExistsAsync(AuthApiFactory factory, string name)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DatabaseService>().FindUserByEmailAsync($"{name}@example.test") is not null;
    }

    private static Task<HttpResponseMessage> PostAsync(AuthApiFactory factory, string path)
    {
        var client = factory.CreateHttpsClient();
        return client.PostAsync($"/test-uow/{path}", content: null);
    }

    [Fact]
    public async Task ARequestThatFinishesNormallyKeepsEverythingItSaved()
    {
        using var factory = new AuthApiFactory(includeProbeControllers: true);

        var response = await PostAsync(factory, "commit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await UserExistsAsync(factory, "uow-commit"));
    }

    [Fact]
    public async Task ARequestThatThrowsAfterSavingLeavesNothingBehind()
    {
        using var factory = new AuthApiFactory(includeProbeControllers: true);

        var response = await PostAsync(factory, "throws");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(await UserExistsAsync(factory, "uow-throws"));
    }

    [Fact]
    public async Task AnAnswerOf401IsNotAFailureToRollBack_SoTheLockoutCounterStillCounts()
    {
        using var factory = new AuthApiFactory(includeProbeControllers: true);

        var response = await PostAsync(factory, "unauthorized");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(await UserExistsAsync(factory, "uow-unauthorized"));
    }

    [Fact]
    public async Task ARefusalThatTheActionCatchesStillFailsTheClick_SoEarlierStepsAreRolledBack()
    {
        using var factory = new AuthApiFactory(includeProbeControllers: true);

        var response = await PostAsync(factory, "swallowed-refusal");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await UserExistsAsync(factory, "uow-swallowed"));
    }

    [Fact]
    public async Task WhenALaterStepIsRefusedTheEarlierStepsOfTheSameClickAreRolledBack()
    {
        using var factory = new AuthApiFactory(includeProbeControllers: true);

        var response = await PostAsync(factory, "second-step-refused");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("never stored", body);
        Assert.DoesNotContain(nameof(GooseWebsite.Api.Modules.Blog.Domain.BlogPost), body);
        Assert.False(await UserExistsAsync(factory, "uow-first-step"));
    }
}
