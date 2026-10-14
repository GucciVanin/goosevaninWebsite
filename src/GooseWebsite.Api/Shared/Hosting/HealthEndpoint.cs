using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Shared.Hosting;

/// <summary>
/// <c>GET /api/health</c>: healthy only when the application answers and its database can be reached.
/// Public and read-only; it reports no error details, only the process start time (so a restart is visible).
/// </summary>
public static class HealthEndpoint
{
    private static readonly DateTime StartedAtUtc = DateTime.UtcNow;

    public sealed record HealthReport(string Status, string Database, DateTime StartedAtUtc);

    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", CheckAsync);
        return endpoints;
    }

    public static async Task<IResult> CheckAsync(DatabaseService database, CancellationToken cancellationToken)
    {
        bool databaseAnswers;
        try
        {
            databaseAnswers = await database.CanConnectAsync(cancellationToken);
        }
        catch (Exception)
        {
            databaseAnswers = false;
        }

        return databaseAnswers
            ? Results.Json(new HealthReport("ok", "ok", StartedAtUtc))
            : Results.Json(new HealthReport("unhealthy", "unavailable", StartedAtUtc), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
