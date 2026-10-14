using Microsoft.AspNetCore.Mvc.Filters;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// One click is one unit of work: everything a request saved is committed together when its action finishes
/// normally and rolled back when the action throws (including a refused write), so nothing from a failed click
/// persists. An action that answers with a failure status (a wrong password, 401) is not an exception: it
/// finishes normally, so state it saved on purpose, such as the failed sign-in counter, is kept.
/// The work is completed here, before the result is written, so the client never sees success for a click
/// that could not be committed. See <see cref="UnitOfWork"/>.
/// </summary>
public sealed class UnitOfWorkFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        await UnitOfWork.CompleteAsync(
            context.HttpContext.RequestServices.GetService<ApplicationDbContext>(),
            requestSucceeded: executed.Exception is null || executed.ExceptionHandled);
    }
}
