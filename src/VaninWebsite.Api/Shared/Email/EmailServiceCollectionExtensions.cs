namespace VaninWebsite.Api.Shared.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>Registers the shared SMTP transport used by every module that sends mail.</summary>
    public static IServiceCollection AddSmtpEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddTransient<ISmtpMailSender, SmtpMailSender>();
        return services;
    }
}
