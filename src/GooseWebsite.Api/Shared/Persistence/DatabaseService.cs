using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Modules.Accounts.Domain;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// The single gateway for every database read and write. Modules keep their own query logic
/// (what to ask for) but never touch <see cref="ApplicationDbContext"/> or the Identity stores
/// directly (how to ask), so cross-cutting safeguards have exactly one place to live.
/// </summary>
/// <remarks>
/// Planned safeguards (input sanitization, access checks) are tracked as story F7-US1 in the
/// product backlog. Add them here, not in callers, so they apply to every module.
/// </remarks>
public sealed class DatabaseService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles)
{
    // ---- Entity access (any module's entities) ----

    /// <summary>A read-only, untracked query over <typeparamref name="T"/>.</summary>
    public IQueryable<T> Read<T>() where T : class => context.Set<T>().AsNoTracking();

    /// <summary>A tracked query, for loading an entity that will be modified and saved.</summary>
    public IQueryable<T> ReadForUpdate<T>() where T : class => context.Set<T>();

    public async Task<T?> FindAsync<T>(params object[] keyValues) where T : class =>
        await context.Set<T>().FindAsync(keyValues);

    public void Add<T>(T entity) where T : class => context.Set<T>().Add(entity);

    public void Remove<T>(T entity) where T : class => context.Set<T>().Remove(entity);

    public Task SaveChangesAsync() => context.SaveChangesAsync();

    // ---- Account store (users and roles) ----

    public string? NormalizeEmail(string email) => users.NormalizeEmail(email);

    public Task<ApplicationUser?> FindUserByEmailAsync(string email) => users.FindByEmailAsync(email);

    public Task<ApplicationUser?> FindUserByIdAsync(string userId) => users.FindByIdAsync(userId);

    public Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal) => users.GetUserAsync(principal);

    public Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password) =>
        users.CreateAsync(user, password);

    public Task<IdentityResult> DeleteUserAsync(ApplicationUser user) => users.DeleteAsync(user);

    public Task<IdentityResult> AddUserToRoleAsync(ApplicationUser user, string role) =>
        users.AddToRoleAsync(user, role);

    public Task<bool> IsUserInRoleAsync(ApplicationUser user, string role) => users.IsInRoleAsync(user, role);

    public Task<IdentityResult> ConfirmUserEmailAsync(ApplicationUser user, string token) =>
        users.ConfirmEmailAsync(user, token);

    public Task<string> GenerateUserEmailConfirmationTokenAsync(ApplicationUser user) =>
        users.GenerateEmailConfirmationTokenAsync(user);

    public Task<bool> RoleExistsAsync(string role) => roles.RoleExistsAsync(role);

    public Task<IdentityResult> CreateRoleAsync(string role) => roles.CreateAsync(new IdentityRole(role));
}
