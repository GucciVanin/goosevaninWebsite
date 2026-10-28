namespace GooseWebsite.Api.Shared.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Registers the one mail service and its two transports (SMTP for rendered mail, MailerSend for
    /// template mail). Settings live under <c>Email:Smtp</c> and <c>Email:MailerSend</c>; the older
    /// <c>MailerSend</c> section is still read so existing secrets and environment keep working.
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<MailerSendOptions>(configuration.GetSection(MailerSendOptions.LegacySectionName));
        services.Configure<MailerSendOptions>(configuration.GetSection(MailerSendOptions.SectionName));

        services.AddTransient<IRenderedMailTransport, SmtpMailTransport>();
        services.AddHttpClient<ITemplateMailTransport, MailerSendTransport>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddTransient<IMailService, MailService>();
        return services;
    }
}
