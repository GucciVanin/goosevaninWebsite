namespace GooseWebsite.Api.Shared.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>Registers the shared SMTP transport used by every module that sends mail.</summary>
    public static IServiceCollection AddSmtpEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddTransient<ISmtpMailSender, SmtpMailSender>();
        return services;
    }

    /// <summary>Registers the MailerSend HTTP API client used to send templated email.</summary>
    public static IServiceCollection AddMailerSendEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MailerSendOptions>(configuration.GetSection(MailerSendOptions.SectionName));
        services.AddHttpClient<IMailerSendClient, MailerSendClient>(client => client.Timeout = TimeSpan.FromSeconds(30));
        return services;
    }
}
