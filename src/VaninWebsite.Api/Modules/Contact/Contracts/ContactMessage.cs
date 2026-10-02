using System.ComponentModel.DataAnnotations;

namespace VaninWebsite.Api.Modules.Contact.Contracts;

// Defines the validated data boundary for public contact submissions.
public sealed class ContactMessage
{
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, StringLength(40)] public string Reason { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Message { get; set; } = string.Empty;
    public string? Website { get; set; }
}