namespace GooseWebsite.Api.Shared.Email;

/// <summary>A recipient (or reply-to) of an email.</summary>
public sealed record MailAddressee(string Email, string Name);

/// <summary>An email whose content the caller has already rendered: HTML plus a plain-text alternative.</summary>
public sealed record RenderedMail(MailAddressee To, string Subject, string Html, string PlainText);

/// <summary>
/// An email rendered by a template stored at the provider. <paramref name="Variables"/> must contain every
/// variable the template uses (names are case-sensitive); a missing one renders as the variable name.
/// </summary>
public sealed record TemplateMail(
    string TemplateId,
    string Subject,
    MailAddressee To,
    IReadOnlyDictionary<string, string> Variables,
    MailAddressee? ReplyTo = null);

/// <summary>Why a send did not succeed. Categories only: never provider text.</summary>
public enum MailFailure
{
    None,
    NotConfigured,
    Rejected,
    TransportError
}

/// <summary>The outcome of one send. Callers get this; they never get an exception.</summary>
public readonly record struct MailResult(bool Succeeded, MailFailure Failure)
{
    public static MailResult Success => new(true, MailFailure.None);

    public static MailResult NotConfigured => new(false, MailFailure.NotConfigured);

    public static MailResult Failed(MailFailure failure) => new(false, failure);
}
