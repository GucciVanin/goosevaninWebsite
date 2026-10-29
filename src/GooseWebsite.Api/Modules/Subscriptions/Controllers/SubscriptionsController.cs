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
}
