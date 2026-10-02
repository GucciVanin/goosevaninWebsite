using System.ComponentModel.DataAnnotations;

namespace VaninWebsite.Api.Modules.Accounts.Contracts;

// Defines the validated identity data accepted by account creation.
public sealed class RegisterRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(12)] public string Password { get; set; } = string.Empty;
    [Required, StringLength(100)] public string DisplayName { get; set; } = string.Empty;
}