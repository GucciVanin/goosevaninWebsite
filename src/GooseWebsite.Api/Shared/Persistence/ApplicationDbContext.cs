using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Modules.Accounts.Domain;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// The single database context. It holds the Identity tables and discovers every module's
/// <see cref="IEntityTypeConfiguration{TEntity}"/>, so a module adds or removes its tables
/// without edits here. It also applies each entity's access rule (<see cref="EntityAccessRule{T}"/>):
/// the read filters here, and the write checks in <see cref="DataSafeguardsInterceptor"/>.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentCaller? caller = null)
    : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>Who is asking. Code outside a web request (and tests that do not say) is the system.</summary>
    public ICurrentCaller Caller { get; } = caller ?? SystemCaller.Instance;

    /// <summary>Read by the query filters: administrators and the system see rows that are hidden from everyone else.</summary>
    public bool CanReadEverything => Caller.Kind is CallerKind.Administrator or CallerKind.System;

    /// <summary>Set when a save in this unit of work was refused or failed, so the click is rolled back even if the code caught the exception.</summary>
    internal bool UnitOfWorkFailed { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(DataSafeguardsInterceptor.Instance);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var rule in AccessRuleCatalog.All.Where(rule => rule.VisibleToEveryone is not null))
        {
            builder.Entity(rule.EntityType).HasQueryFilter(CallerAwareFilter(rule.VisibleToEveryone!));
        }
    }

    // "visible to everyone || the caller may read everything", re-evaluated for each context instance.
    private LambdaExpression CallerAwareFilter(LambdaExpression visibleToEveryone)
    {
        var entity = visibleToEveryone.Parameters[0];
        var canReadEverything = Expression.Property(Expression.Constant(this), nameof(CanReadEverything));
        return Expression.Lambda(Expression.OrElse(visibleToEveryone.Body, canReadEverything), entity);
    }
}
