using Microsoft.AspNetCore.Diagnostics;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// Turns the persistence module's refusals into plain answers: 403 when the caller may not make the change,
/// 400 when some text was not acceptable. The messages are fixed and never echo what was submitted.
/// </summary>
public sealed class DataSafeguardExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case DataAccessDeniedException:
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await httpContext.Response.WriteAsJsonAsync(new { error = "You are not allowed to do that." }, cancellationToken);
                return true;
            case DataRejectedException:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(new { error = "Some of the text could not be accepted." }, cancellationToken);
                return true;
            default:
                return false;
        }
    }
}
