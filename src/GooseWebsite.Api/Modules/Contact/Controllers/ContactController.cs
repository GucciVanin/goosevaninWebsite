using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Contact.Contracts;
using GooseWebsite.Api.Modules.Contact.Models;
using GooseWebsite.Api.Modules.Contact.Services;

namespace GooseWebsite.Api.Modules.Contact.Controllers;

// Keeps public contact validation, abuse controls, and verification sequencing together.
[ApiController]
[Route("api/[controller]")]
public sealed class ContactController : ControllerBase
{
    private static readonly string[] AllowedReasons = ["Work or collaboration", "Personal note"];
    private readonly IContactVerificationService _verificationService;
    private readonly IContactEmailDeliveryService _emailDeliveryService;
    private readonly IContactSubmissionAbuseGuard _submissionAbuseGuard;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        IContactVerificationService verificationService,
        IContactEmailDeliveryService emailDeliveryService,
        IContactSubmissionAbuseGuard submissionAbuseGuard,
        ILogger<ContactController> logger)
    {
        _verificationService = verificationService;
        _emailDeliveryService = emailDeliveryService;
        _submissionAbuseGuard = submissionAbuseGuard;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ContactMessage message)
    {
        var clientKey = GetClientKey();

        if (!string.IsNullOrWhiteSpace(message.Website))
        {
            _logger.LogWarning("Contact submission rejected as honeypot traffic; clientKey={ClientKey}; reason={Reason}", clientKey, message.Reason);
            return BadRequest(new { error = "Your message could not be processed." });
        }

        if (string.IsNullOrWhiteSpace(message.Name) || string.IsNullOrWhiteSpace(message.Email) || string.IsNullOrWhiteSpace(message.Reason) || string.IsNullOrWhiteSpace(message.Message))
        {
            return BadRequest(new { error = "Name, email, reason, and message are required." });
        }

        if (!AllowedReasons.Contains(message.Reason, StringComparer.Ordinal))
        {
            return BadRequest(new { error = "Please choose a valid reason." });
        }

        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(message.Email))
        {
            return BadRequest(new { error = "Please provide a valid email address." });
        }

        if (!_submissionAbuseGuard.TryAllow(clientKey, out var rejectionReason))
        {
            _logger.LogWarning("Contact submission rate limited; clientKey={ClientKey}; rejectionReason={RejectionReason}; reason={Reason}", clientKey, rejectionReason, message.Reason);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many contact submissions have been received from this device. Please try again later." });
        }

        // The form emails whatever address is typed, so cap how often one address can be targeted.
        if (!_submissionAbuseGuard.TryAllowRecipient(message.Email, out var recipientRejection))
        {
            _logger.LogWarning("Contact submission rate limited for recipient; clientKey={ClientKey}; rejectionReason={RejectionReason}", clientKey, recipientRejection);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many verification emails have been requested for this address. Please check your inbox or try again later." });
        }

        // The link goes only to the submitted address; it is never returned to the caller, so
        // only someone who controls that inbox can confirm it.
        var verification = _verificationService.CreateVerification(message);
        var emailed = false;
        try
        {
            emailed = await _emailDeliveryService.SendVerificationRequestAsync(message.Name, message.Email, verification.VerificationUrl);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            // An address the mail library rejects counts as a failed send; never keep the pending message.
            _logger.LogWarning("Contact verification email was rejected as malformed; exceptionType={ExceptionType}", exception.GetType().Name);
        }

        if (!emailed)
        {
            _verificationService.Discard(verification.Token);
            _logger.LogWarning("Contact verification email could not be sent; clientKey={ClientKey}; reason={Reason}", clientKey, message.Reason);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "The verification email could not be sent. Please try again later or email me directly." });
        }

        _logger.LogInformation("Contact verification request queued; clientKey={ClientKey}; reason={Reason}", clientKey, message.Reason);

        return Ok(new
        {
            received = false,
            verificationRequired = true,
            message = "Thanks. Check your email and confirm your address to send the message."
        });
    }

    // POST, not GET, so mail link scanners that fetch URLs cannot trigger delivery.
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] ContactVerificationRequest request)
    {
        var outcome = _verificationService.Verify(request.Token);

        if (!outcome.IsSuccess)
        {
            if (outcome.IsExpired)
            {
                return StatusCode(StatusCodes.Status410Gone, new { error = outcome.ErrorMessage ?? "This verification link has expired." });
            }

            if (outcome.IsUsed)
            {
                return Conflict(new { error = outcome.ErrorMessage ?? "This verification link is no longer valid." });
            }

            return BadRequest(new { error = outcome.ErrorMessage ?? "The verification link is invalid." });
        }

        var pending = outcome.PendingMessage!;
        var delivery = await _emailDeliveryService.SendAsync(pending);

        if (delivery != ContactDeliveryOutcome.Delivered)
        {
            // Verify consumed the token, so put the message back or the promised retry would be rejected.
            // If Gustavo was already notified, remember it so the retry only sends the receipt.
            _verificationService.Restore(
                request.Token,
                delivery == ContactDeliveryOutcome.OwnerNotifiedReceiptFailed ? pending with { OwnerNotified = true } : pending);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                verified = true,
                message = "Your email was verified, but the delivery attempt to Gustavo failed. Please try again later."
            });
        }

        return Ok(new
        {
            verified = true,
            message = "Your email has been verified and the message was sent to Gustavo."
        });
    }

    private string GetClientKey()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
