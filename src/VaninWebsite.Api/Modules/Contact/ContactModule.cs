using VaninWebsite.Api.Modules.Contact.Models;
using VaninWebsite.Api.Modules.Contact.Services;

namespace VaninWebsite.Api.Modules.Contact;

/// <summary>
/// Contact module: verification-first public contact form with abuse controls. Holds pending
/// messages in memory only; there is no durable inbox.
/// </summary>
public static class ContactModule
{
    public static IServiceCollection AddContactModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ContactOptions>(configuration.GetSection(ContactOptions.SectionName));
        services.AddSingleton<IContactVerificationService, ContactVerificationService>();
        services.AddSingleton<IContactSubmissionAbuseGuard, ContactSubmissionAbuseGuard>();
        services.AddTransient<IContactEmailDeliveryService, ContactEmailDeliveryService>();
        return services;
    }
}
