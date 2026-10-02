namespace VaninWebsite.Api.Shared.Modules;

/// <summary>
/// An operator command-line entry point owned by a module (for example <c>admin provision</c>).
/// </summary>
public interface IModuleCommand
{
    /// <summary>Runs the command when <paramref name="args"/> match it; returns false otherwise.</summary>
    Task<bool> TryRunAsync(string[] args);
}
