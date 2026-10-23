using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GooseWebsite.Api.Modules.Accounts.Contracts;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Accounts.Services;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Accounts.Controllers;

// Centralizes identity endpoints so authentication policy stays consistent. Reader registration and
// Verification rules live in IReaderAccountService; this controller maps their outcomes to responses.
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    DatabaseService database,
    SignInManager<ApplicationUser> signInManager,
    IReaderAccountService readers,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IAntiforgery antiforgery) : ControllerBase
{
    private const string GenericSignInError = "Unable to sign in with those credentials.";
    private const string VerificationPendingMessage = "If this account is eligible, you will receive a confirmation email.";
    private const string InvalidVerificationMessage = "Your verification link is invalid or expired.";

    // A hint for the browser app, not a credential: present while a sign-in is believed to be active, so the app
    // asks GET api/auth/me only then and a signed-out visitor never triggers a 401. It holds a constant, never an
    // identity or a token, and grants nothing: the HttpOnly session cookie remains the only proof of a session.
    internal const string SignedInMarkerCookie = "goosewebsite.signedin";

    private static readonly ApplicationUser DummyUser = new();

    // Equalizes unknown-user and locked-account work to reduce credential-timing leaks.
    private static readonly Lazy<string> DummyPasswordHash = new(() =>
        new PasswordHasher<ApplicationUser>().HashPassword(DummyUser, Guid.NewGuid().ToString("N")));

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
        return Ok(new { enabled = readers.RegistrationEnabled });
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

        Response.Cookies.Append(SignedInMarkerCookie, "1", SignedInMarkerOptions());
        return Ok(await ToResponseAsync(user));
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountsModule.ReaderAccountEmailRateLimitPolicy)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Every outcome below answers identically so callers cannot probe which emails exist.
        return await readers.RegisterAsync(request, ModelState.IsValid) switch
        {
            ReaderRegistrationOutcome.Disabled => NotFound(),
            ReaderRegistrationOutcome.InvalidInput => Ok(new { message = "Please check your email to confirm your account." }),
            _ => Ok(new { message = VerificationPendingMessage })
        };
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountsModule.ReaderAccountEmailRateLimitPolicy)]
    [HttpPost("verification/resend")]
    public async Task<IActionResult> ResendEmailVerification(ResendEmailVerificationRequest request)
    {
        await readers.ResendVerificationAsync(request.Email);
        return Ok(new { message = VerificationPendingMessage });
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountsModule.ReaderAccountEmailRateLimitPolicy)]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        var verified = await readers.VerifyEmailAsync(request.UserId, request.Token);
        return Ok(new
        {
            message = verified ? "Your email address has been verified." : InvalidVerificationMessage
        });
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Clearing the browser's cookie is not enough: a copy captured earlier would still work. Rotating the
        // security stamp ends every session issued before now (the stamp is checked on each request).
        var user = await database.GetUserAsync(User);
        await signInManager.SignOutAsync();
        if (user is not null)
        {
            await database.RotateSecurityStampAsync(user);
        }

        Response.Cookies.Delete(SignedInMarkerCookie, SignedInMarkerOptions());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUserResponse>> Me()
    {
        var user = await database.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await ToResponseAsync(user));
    }

    // Readable by the page's script on purpose (no HttpOnly); a session cookie (no expiry) like the one it mirrors.
    private static CookieOptions SignedInMarkerOptions() => new()
    {
        Path = "/",
        Secure = true,
        SameSite = SameSiteMode.Lax,
        HttpOnly = false,
        IsEssential = true
    };

    private async Task<AuthenticatedUserResponse> ToResponseAsync(ApplicationUser user) =>
        new(user.DisplayName, await database.IsUserInRoleAsync(user, AccountRoles.Admin));

    private void BurnDummyPasswordWork(string password) =>
        _ = passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash.Value, password);
}
