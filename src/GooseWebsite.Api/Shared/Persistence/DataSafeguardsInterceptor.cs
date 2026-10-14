using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// Runs before every save, whichever module asked: refuses writes the caller may not make, normalizes the
/// text users typed, rejects text over its declared limit, and, inside a web request, opens the transaction
/// that <see cref="UnitOfWorkFilter"/> commits or rolls back as one click. Logs say what kind of thing was
/// refused and for which entity, never the values.
/// </summary>
public sealed class DataSafeguardsInterceptor : SaveChangesInterceptor
{
    public static readonly DataSafeguardsInterceptor Instance = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is ApplicationDbContext context)
        {
            Apply(context);
            if (ShouldStartUnitOfWork(context))
            {
                context.Database.BeginTransaction();
            }
        }

        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ApplicationDbContext context)
        {
            Apply(context);
            if (ShouldStartUnitOfWork(context))
            {
                await context.Database.BeginTransactionAsync(cancellationToken);
            }
        }

        return result;
    }

    private static bool ShouldStartUnitOfWork(ApplicationDbContext context) =>
        context.Caller.InHttpRequest && context.Database.CurrentTransaction is null;

    // A save that fails after this point (a constraint, a lost connection) is reported here, so the click is rolled
    // back even when the code that asked for the save caught the exception and answered normally.
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is ApplicationDbContext context)
        {
            context.UnitOfWorkFailed = true;
        }
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        SaveChangesFailed(eventData);
        return Task.CompletedTask;
    }

    private static void Apply(ApplicationDbContext context)
    {
        try
        {
            ApplyRules(context);
        }
        catch
        {
            context.UnitOfWorkFailed = true;
            throw;
        }
    }

    private static void ApplyRules(ApplicationDbContext context)
    {
        var caller = context.Caller.Kind;
        foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            var entityType = entry.Metadata.ClrType;
            var rule = AccessRuleCatalog.RuleFor(entityType);

            // An entity without a rule is refused (fail closed); an architecture test keeps this from happening.
            if (rule is null ? caller != CallerKind.System : !MayWrite(rule.Writes, caller))
            {
                Refuse(context, "write-not-allowed", entityType.Name, caller);
                throw new DataAccessDeniedException(entityType.Name);
            }

            if (rule is not null && entry.State != EntityState.Deleted)
            {
                NormalizeUserText(context, entry, rule, caller);
            }
        }
    }

    private static bool MayWrite(WriteAccess access, CallerKind caller) => access switch
    {
        WriteAccess.Anyone => true,
        WriteAccess.Administrator => caller is CallerKind.Administrator or CallerKind.System,
        _ => caller == CallerKind.System
    };

    private static void NormalizeUserText(ApplicationDbContext context, EntityEntry entry, IEntityAccessRule rule, CallerKind caller)
    {
        foreach (var text in rule.UserText)
        {
            var property = entry.Property(text.Property.Name);
            if (entry.State == EntityState.Modified && !property.IsModified)
            {
                continue;
            }

            if (property.CurrentValue is not string value)
            {
                continue;
            }

            var normalized = Normalize(value);
            if (text.MaxLength is { } max && normalized.Length > max)
            {
                Refuse(context, "text-over-limit", rule.EntityType.Name, caller, text.Property.Name);
                throw new DataRejectedException(rule.EntityType.Name, text.Property.Name);
            }

            if (!string.Equals(normalized, value, StringComparison.Ordinal))
            {
                property.CurrentValue = normalized;
            }
        }
    }

    // Trim, and drop control characters other than line breaks and tabs.
    private static string Normalize(string value)
    {
        var kept = value.Where(character => !char.IsControl(character) || character is '\n' or '\r' or '\t').ToArray();
        return new string(kept).Trim();
    }

    private static void Refuse(ApplicationDbContext context, string kind, string entity, CallerKind caller, string? property = null)
    {
        var logger = context.GetService<ILoggerFactory>().CreateLogger("GooseWebsite.DataSafeguards");
        logger.LogWarning(
            "Data operation refused; kind={Kind}; entity={Entity}; property={Property}; caller={Caller}",
            kind,
            entity,
            property ?? "-",
            caller);
    }
}
