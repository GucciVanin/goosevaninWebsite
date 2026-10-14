namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// Ends the transaction that the first save of a web request opened: commit when the click succeeded, roll back
/// when it failed. A click has failed when the request threw, or when any save in it was refused or failed, even if
/// the code caught that exception and answered normally. Commit and rollback ignore a client that disconnected:
/// once the server has finished the click, it stays finished.
/// </summary>
internal static class UnitOfWork
{
    public static async Task CompleteAsync(ApplicationDbContext? database, bool requestSucceeded)
    {
        var transaction = database?.Database.CurrentTransaction;
        if (database is null || transaction is null)
        {
            return;
        }

        if (requestSucceeded && !database.UnitOfWorkFailed)
        {
            await transaction.CommitAsync(CancellationToken.None);
        }
        else
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
    }
}

/// <summary>
/// Safety net for saves made outside an MVC action (middleware, minimal endpoints): the same commit or rollback,
/// applied when the request ends. For actions, <see cref="UnitOfWorkFilter"/> has already completed the work
/// before the result is written, so there is nothing left to do here.
/// </summary>
public sealed class UnitOfWorkMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch
        {
            await UnitOfWork.CompleteAsync(context.RequestServices.GetService<ApplicationDbContext>(), requestSucceeded: false);
            throw;
        }

        await UnitOfWork.CompleteAsync(context.RequestServices.GetService<ApplicationDbContext>(), requestSucceeded: true);
    }
}

public static class UnitOfWorkApplicationBuilderExtensions
{
    /// <summary>Place it right after the exception handler, so a failing request is rolled back before it is answered.</summary>
    public static IApplicationBuilder UseUnitOfWork(this IApplicationBuilder app) => app.UseMiddleware<UnitOfWorkMiddleware>();
}
