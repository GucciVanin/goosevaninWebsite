using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace GooseWebsite.Api.Shared.Email;

/// <summary>
/// The only place the application talks to the MailerSend HTTP API. It reports categories back to
/// <see cref="MailService"/>; it never logs.
/// </summary>
public sealed class MailerSendTransport(HttpClient http, IOptions<MailerSendOptions> options) : ITemplateMailTransport
{
    private readonly MailerSendOptions _options = options.Value;

    public string Name => "MailerSend";

    public async Task<MailTransportOutcome> SendTemplateAsync(TemplateMail email, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return MailTransportOutcome.NotConfigured;
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

            // Report the status code only: error bodies can echo recipient addresses.
            var diagnostics = $"templateId={email.TemplateId}; httpStatus={(int)response.StatusCode}";
            return response.IsSuccessStatusCode
                ? new MailTransportOutcome(MailFailure.None, diagnostics)
                : new MailTransportOutcome(MailFailure.Rejected, diagnostics);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new MailTransportOutcome(MailFailure.TransportError, $"templateId={email.TemplateId}; exceptionType={exception.GetType().Name}");
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
