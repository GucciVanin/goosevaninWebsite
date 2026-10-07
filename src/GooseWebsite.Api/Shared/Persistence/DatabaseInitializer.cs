using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Shared.Modules;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// Creates the schema before any module initializer runs.
/// </summary>
/// <remarks>
/// Uses <c>EnsureCreated</c>, which cannot evolve an existing schema. Replace it with EF Core
/// migrations before the first schema change on a database that holds production data.
/// </remarks>
public sealed class DatabaseInitializer : IModuleInitializer
{
    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        return context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
