namespace GooseWebsite.Api.Shared.Email;

/// <summary>A recipient (or reply-to) of an email.</summary>
public sealed record MailAddressee(string Email, string Name);

/// <summary>
/// An email whose content the caller has already rendered: HTML plus a plain-text alternative. <paramref name="Headers"/>
/// are extra message headers (for example <c>List-Unsubscribe</c>); null means none. <paramref name="ReplyTo"/> is
/// written as the Reply-To header when given.
/// </summary>
public sealed record RenderedMail(
    MailAddressee To,
    string Subject,
    string Html,
    string PlainText,
    IReadOnlyDictionary<string, string>? Headers = null,
    MailAddressee? ReplyTo = null);

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
