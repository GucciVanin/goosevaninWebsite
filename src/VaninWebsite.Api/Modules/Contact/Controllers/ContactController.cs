using Microsoft.AspNetCore.Mvc;
using VaninWebsite.Api.Modules.Contact.Contracts;
using VaninWebsite.Api.Modules.Contact.Services;

namespace VaninWebsite.Api.Modules.Contact.Controllers;

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
    public IActionResult Send([FromBody] ContactMessage message)
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

        var verification = _verificationService.CreateVerification(message);
        _logger.LogInformation("Contact verification request queued; clientKey={ClientKey}; reason={Reason}; verificationTokenCreated={VerificationTokenCreated}", clientKey, message.Reason, true);

        return Ok(new
        {
            received = false,
            verificationRequired = true,
            verificationToken = verification.Token,
            verificationUrl = verification.VerificationUrl,
            message = "Thanks. I need to verify your email before the message is sent."
        });
    }

    [HttpGet("verify")]
    public async Task<IActionResult> Verify([FromQuery] string token)
    {
        var outcome = _verificationService.Verify(token);

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

        var delivered = await _emailDeliveryService.SendAsync(outcome.PendingMessage!);

        if (!delivered)
        {
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
