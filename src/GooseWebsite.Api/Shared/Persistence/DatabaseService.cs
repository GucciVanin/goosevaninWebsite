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
/// The safeguards (story F7-US1) are not in this class but beneath it, where no caller can switch them off:
/// the read filters and the write interceptor of <see cref="ApplicationDbContext"/>, driven by each entity's
/// <see cref="EntityAccessRule{T}"/>. A module declares a rule next to its entity and keeps its own queries.
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

    // There is deliberately no Find-by-key: it would skip the read filters. Use ReadForUpdate and filter by key.

    public void Add<T>(T entity) where T : class => context.Set<T>().Add(entity);

    public void Remove<T>(T entity) where T : class => context.Set<T>().Remove(entity);

    public Task SaveChangesAsync() => context.SaveChangesAsync();

    /// <summary>True when the database answers; used by the health check.</summary>
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
        context.Database.CanConnectAsync(cancellationToken);

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

    /// <summary>
    /// Replaces a user's password without the old one (operator recovery). Changes the security stamp so
    /// existing sessions end, and clears any lockout so the new password can be used straight away.
    /// </summary>
    public async Task<IdentityResult> ResetUserPasswordAsync(ApplicationUser user, string newPassword)
    {
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var result = await users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return result;
        }

        await users.SetLockoutEndDateAsync(user, null);
        await users.ResetAccessFailedCountAsync(user);
        return result;
    }

    /// <summary>Invalidates every session cookie issued to the user so far (they are checked against the stamp).</summary>
    public Task<IdentityResult> RotateSecurityStampAsync(ApplicationUser user) => users.UpdateSecurityStampAsync(user);

    public Task<bool> RoleExistsAsync(string role) => roles.RoleExistsAsync(role);

    public Task<IdentityResult> CreateRoleAsync(string role) => roles.CreateAsync(new IdentityRole(role));
}
