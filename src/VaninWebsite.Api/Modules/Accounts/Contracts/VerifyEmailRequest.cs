using System.ComponentModel.DataAnnotations;

namespace VaninWebsite.Api.Modules.Accounts.Contracts;

// Defines the identity and token pair needed to confirm an email address.
public sealed class VerifyEmailRequest
{
    [Required] public string UserId { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
}