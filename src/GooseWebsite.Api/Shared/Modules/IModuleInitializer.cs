namespace GooseWebsite.Api.Shared.Modules;

/// <summary>
/// Lets a module run its own startup work (for example seeding roles) after the database exists,
/// without <c>Program.cs</c> needing to know the module's details.
/// </summary>
public interface IModuleInitializer
{
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
}
