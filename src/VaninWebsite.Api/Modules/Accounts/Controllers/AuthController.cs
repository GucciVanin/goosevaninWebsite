using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VaninWebsite.Api.Modules.Accounts.Contracts;
using VaninWebsite.Api.Modules.Accounts.Domain;
using VaninWebsite.Api.Modules.Accounts.Services;
using VaninWebsite.Api.Shared.Persistence;

namespace VaninWebsite.Api.Modules.Accounts.Controllers;

// Centralizes identity endpoints so authentication policy stays consistent.
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    DatabaseService database,
    SignInManager<ApplicationUser> signInManager,
    IAccountEmailSender accountEmailSender,
    IConfiguration configuration,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IAntiforgery antiforgery) : ControllerBase
{
    private const string GenericSignInError = "Unable to sign in with those credentials.";
    private const string VerificationPendingMessage = "If this account is eligible, you will receive a confirmation email.";
    private const string InvalidVerificationMessage = "Your verification link is invalid or expired.";

    private static readonly ApplicationUser DummyUser = new();

    // Equalizes unknown-user and locked-account work to reduce credential-timing leaks.
    private static readonly Lazy<string> DummyPasswordHash = new(() =>
        new PasswordHasher<ApplicationUser>().HashPassword(DummyUser, Guid.NewGuid().ToString("N")));

    private bool IsReaderRegistrationEnabled => configuration.GetValue("Auth:ReaderRegistration:Enabled", false);

    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [AllowAnonymous]
    [HttpGet("reader-registration")]
    public IActionResult GetReaderRegistrationAvailability()
    {
        return Ok(new { enabled = IsReaderRegistrationEnabled });
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticatedUserResponse>> Login(LoginRequest request)
    {
        BurnDummyPasswordWork(request.Password);
        var user = await database.FindUserByEmailAsync(request.Email);
        if (user is null)
        {
            BurnDummyPasswordWork(request.Password);
            return Unauthorized(new { error = GenericSignInError });
        }

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            BurnDummyPasswordWork(request.Password);
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new { error = GenericSignInError });
        }

        return Ok(await ToResponseAsync(user));
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountsModule.ReaderAccountEmailRateLimitPolicy)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!IsReaderRegistrationEnabled)
        {
            return NotFound();
        }

        // Every outcome below answers identically so callers cannot probe which emails exist.
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Ok(new { message = "Please check your email to confirm your account." });
        }

        var email = request.Email.Trim();
        if (await database.FindUserByEmailAsync(email) is not null)
        {
            return Ok(new { message = VerificationPendingMessage });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = false
        };

        if (!(await database.CreateUserAsync(user, request.Password)).Succeeded)
        {
            return Ok(new { message = VerificationPendingMessage });
        }

        if (!(await database.AddUserToRoleAsync(user, AccountRoles.Reader)).Succeeded)
        {
            await database.DeleteUserAsync(user);
            return Ok(new { message = VerificationPendingMessage });
        }

        await SendVerificationEmailAsync(user);
        return Ok(new { message = VerificationPendingMessage });
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountsModule.ReaderAccountEmailRateLimitPolicy)]
    [HttpPost("verification/resend")]
    public async Task<IActionResult> ResendEmailVerification(ResendEmailVerificationRequest request)
    {
        var user = await database.FindUserByEmailAsync(request.Email.Trim());
        if (user is not null
            && !user.EmailConfirmed
            && await database.IsUserInRoleAsync(user, AccountRoles.Reader))
        {
            await SendVerificationEmailAsync(user);
        }

        return Ok(new { message = VerificationPendingMessage });
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountsModule.ReaderAccountEmailRateLimitPolicy)]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Token))
        {
            return Ok(new { message = InvalidVerificationMessage });
        }

        var user = await database.FindUserByIdAsync(request.UserId);
        if (user is null || user.EmailConfirmed)
        {
            return Ok(new { message = InvalidVerificationMessage });
        }

        var result = await database.ConfirmUserEmailAsync(user, request.Token);
        return Ok(new
        {
            message = result.Succeeded ? "Your email address has been verified." : InvalidVerificationMessage
        });
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUserResponse>> Me()
    {
        var user = await database.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await ToResponseAsync(user));
    }

    private async Task<AuthenticatedUserResponse> ToResponseAsync(ApplicationUser user) =>
        new(user.DisplayName, await database.IsUserInRoleAsync(user, AccountRoles.Admin));

    private void BurnDummyPasswordWork(string password) =>
        _ = passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash.Value, password);

    private async Task SendVerificationEmailAsync(ApplicationUser user)
    {
        var token = await database.GenerateUserEmailConfirmationTokenAsync(user);
        var publicBaseUrl = configuration["PublicBaseUrl"]?.TrimEnd('/') ?? "https://localhost";

        // The token travels in the URL fragment so it is never sent in the initial HTTP request.
        var verificationUrl = $"{publicBaseUrl}/verify-email#userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";
        await accountEmailSender.SendVerificationEmailAsync(user.Email!, user.DisplayName, verificationUrl);
    }
}
