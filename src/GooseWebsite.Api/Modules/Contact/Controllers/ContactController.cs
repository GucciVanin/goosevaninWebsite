using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Modules.Contact.Services;

namespace GooseWebsite.Api.Modules.Contact.Controllers;

// Translates contact outcomes into HTTP responses; the rules live in the Contact submission module.
[ApiController]
[Route("api/[controller]")]
public sealed class ContactController(IContactSubmissionService contact) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ContactMessage message)
    {
        var outcome = await contact.SubmitAsync(message, GetClientKey());

        return outcome.Status switch
        {
            ContactSubmitStatus.Accepted => Ok(new
            {
                received = false,
                verificationRequired = true,
                message = "Thanks. Check your email and confirm your address to send the message."
            }),
            ContactSubmitStatus.Honeypot => BadRequest(new { error = "Your message could not be processed." }),
            ContactSubmitStatus.MissingFields => BadRequest(new { error = "Name, email, reason, and message are required." }),
            ContactSubmitStatus.InvalidReason => BadRequest(new { error = "Please choose a valid reason." }),
            ContactSubmitStatus.InvalidEmail => BadRequest(new { error = "Please provide a valid email address." }),
            ContactSubmitStatus.ClientRateLimited => StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many contact submissions have been received from this device. Please try again later." }),
            ContactSubmitStatus.RecipientRateLimited => StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many verification emails have been requested for this address. Please check your inbox or try again later." }),
            ContactSubmitStatus.EmailNotSent => StatusCode(StatusCodes.Status502BadGateway, new { error = "The verification email could not be sent. Please try again later or email me directly." }),
            _ => throw new InvalidOperationException($"Unmapped contact submit status {outcome.Status}.")
        };
    }

    // POST, not GET, so mail link scanners that fetch URLs cannot trigger delivery.
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] ContactVerificationRequest request)
    {
        var outcome = await contact.VerifyAsync(request.Token);

        return outcome.Status switch
        {
            ContactVerifyStatus.Delivered => Ok(new
            {
                verified = true,
                message = "Your email has been verified and the message was sent to Gustavo."
            }),
            ContactVerifyStatus.DeliveryFailed => StatusCode(StatusCodes.Status502BadGateway, new
            {
                verified = true,
                message = "Your email was verified, but the delivery attempt to Gustavo failed. Please try again later."
            }),
            ContactVerifyStatus.Expired => StatusCode(StatusCodes.Status410Gone, new { error = outcome.ErrorMessage ?? "This verification link has expired." }),
            ContactVerifyStatus.UnknownOrAlreadyUsed => Conflict(new { error = outcome.ErrorMessage ?? "This verification link is no longer valid." }),
            ContactVerifyStatus.Invalid => BadRequest(new { error = outcome.ErrorMessage ?? "The verification link is invalid." }),
            _ => throw new InvalidOperationException($"Unmapped contact verify status {outcome.Status}.")
        };
    }

    private string GetClientKey()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
