using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VaninWebsite.Api.Modules.Accounts.Domain;

namespace VaninWebsite.Api.Shared.Persistence;

/// <summary>
/// The single database context. It holds the Identity tables and discovers every module's
/// <see cref="IEntityTypeConfiguration{TEntity}"/>, so a module adds or removes its tables
/// without edits here.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
