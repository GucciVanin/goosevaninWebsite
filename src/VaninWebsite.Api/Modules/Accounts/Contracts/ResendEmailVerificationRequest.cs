using System.ComponentModel.DataAnnotations;

namespace VaninWebsite.Api.Modules.Accounts.Contracts;

// Defines the minimum account information needed to request another confirmation email.
public sealed class ResendEmailVerificationRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}