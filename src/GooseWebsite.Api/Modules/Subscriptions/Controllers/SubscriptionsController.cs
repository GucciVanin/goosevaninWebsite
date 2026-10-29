using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GooseWebsite.Api.Modules.Subscriptions.Contracts;
using GooseWebsite.Api.Modules.Subscriptions.Services;

namespace GooseWebsite.Api.Modules.Subscriptions.Controllers;

// Translates subscription requests into HTTP answers; the rules live in ISubscriptionService. Public by design:
// visitors subscribe without an account, and double opt-in (the emailed link) proves they own the address.
[AllowAnonymous]
[ApiController]
[Route("api/subscriptions")]
public sealed class SubscriptionsController(ISubscriptionService subscriptions) : ControllerBase
{
    // One message for every outcome (new, pending, confirmed, invalid, honeypot), so an address cannot be probed.
    private const string SubscribeMessage =
        "Thanks. If that address can be subscribed, a confirmation email is on its way. Open the link in it to finish.";

    private const string InvalidLinkMessage = "This confirmation link is no longer valid.";
    private const string InvalidUnsubscribeMessage = "This unsubscribe link is no longer valid.";
    private const string OneClickValue = "One-Click";

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(SubscriptionsModule.SubscribeRateLimitPolicy)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        await subscriptions.SubscribeAsync(request);
        return Ok(new { message = SubscribeMessage });
    }

    // POST, not GET, so mail link scanners that fetch URLs cannot confirm anyone.
    [HttpPost("confirm")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(SubscriptionsModule.ConfirmRateLimitPolicy)]
    public async Task<IActionResult> Confirm([FromBody] ConfirmSubscriptionRequest request)
    {
        return await subscriptions.ConfirmAsync(request.Token)
            ? Ok(new { confirmed = true, message = "Your subscription is confirmed. Thank you." })
            : BadRequest(new { error = InvalidLinkMessage });
    }

    // The page behind the emailed link posts here after the person presses the button. POST with a CSRF token,
    // never GET, so a mail scanner that fetches the link cannot unsubscribe anyone.
    [HttpPost("unsubscribe")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(SubscriptionsModule.ConfirmRateLimitPolicy)]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        return await subscriptions.UnsubscribeAsync(request.Token)
            ? Ok(new { unsubscribed = true, message = "You are unsubscribed and your name and email address are deleted." })
            : BadRequest(new { error = InvalidUnsubscribeMessage });
    }

    // RFC 8058 one-click unsubscribe, the target of the List-Unsubscribe header. Mail clients POST here without a
    // session, so there is no CSRF token; the signed token in the address is the credential and the body must be the
    // fixed "List-Unsubscribe=One-Click" the standard defines. GET is not mapped, so it answers 405 and changes nothing.
    // It buffers almost nothing (a body of a kilobyte and two small form fields) and has its own, higher rate limit:
    // mail providers post for many subscribers from a few addresses. Its query string holds the token, so the
    // address must never be logged (Docs/architecture.md).
    [HttpPost("unsubscribe/one-click")]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(1024)]
    [RequestFormLimits(ValueCountLimit = 2, KeyLengthLimit = 64, ValueLengthLimit = 64, MultipartBodyLengthLimit = 1024)]
    [EnableRateLimiting(SubscriptionsModule.OneClickRateLimitPolicy)]
    public async Task<IActionResult> UnsubscribeOneClick([FromQuery] string? token, [FromForm(Name = "List-Unsubscribe")] string? marker)
    {
        if (!string.Equals(marker, OneClickValue, StringComparison.Ordinal))
        {
            return BadRequest(new { error = InvalidUnsubscribeMessage });
        }

        return await subscriptions.UnsubscribeAsync(token)
            ? Ok(new { unsubscribed = true })
            : BadRequest(new { error = InvalidUnsubscribeMessage });
    }
}
