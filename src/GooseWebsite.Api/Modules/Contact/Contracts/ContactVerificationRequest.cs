using System.ComponentModel.DataAnnotations;

namespace GooseWebsite.Api.Modules.Contact.Contracts;

// Carries the token from the emailed link so it never appears in a query string or server log.
public sealed class ContactVerificationRequest
{
    [Required, StringLength(200)] public string Token { get; set; } = string.Empty;
}
