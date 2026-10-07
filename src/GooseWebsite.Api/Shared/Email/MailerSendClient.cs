using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace GooseWebsite.Api.Shared.Email;

public sealed class MailerSendClient(HttpClient http, IOptions<MailerSendOptions> options, ILogger<MailerSendClient> logger) : IMailerSendClient
{
    private readonly MailerSendOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<bool> SendTemplateAsync(MailerSendTemplateEmail email, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            logger.LogWarning("MailerSend configuration is incomplete; email delivery was skipped.");
            return false;
        }

        var payload = new SendRequest(
            new Address(_options.FromEmail, _options.FromName),
            [new Address(email.To.Email, email.To.Name)],
            email.ReplyTo is null ? null : new Address(email.ReplyTo.Email, email.ReplyTo.Name),
            email.Subject,
            email.TemplateId,
            [new Personalization(email.To.Email, email.Variables)]);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_options.BaseUrl), "email"))
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);

            // Log the status code only: error bodies can echo recipient addresses.
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("MailerSend delivery failed; templateId={TemplateId}; httpStatus={HttpStatus}.", email.TemplateId, (int)response.StatusCode);
                return false;
            }

            logger.LogInformation("MailerSend delivery accepted; templateId={TemplateId}; httpStatus={HttpStatus}.", email.TemplateId, (int)response.StatusCode);
            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogError("MailerSend delivery failed; templateId={TemplateId}; exceptionType={ExceptionType}.", email.TemplateId, exception.GetType().Name);
            return false;
        }
    }

    private sealed record Address(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string Name);

    private sealed record Personalization(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("data")] IReadOnlyDictionary<string, string> Data);

    private sealed record SendRequest(
        [property: JsonPropertyName("from")] Address From,
        [property: JsonPropertyName("to")] Address[] To,
        [property: JsonPropertyName("reply_to"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Address? ReplyTo,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("template_id")] string TemplateId,
        [property: JsonPropertyName("personalization")] Personalization[] Personalization);
}
