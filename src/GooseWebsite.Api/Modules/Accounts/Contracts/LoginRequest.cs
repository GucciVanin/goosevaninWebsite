using System.ComponentModel.DataAnnotations;

namespace GooseWebsite.Api.Modules.Accounts.Contracts;

// Defines the validated credentials accepted by the sign-in endpoint.
public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}