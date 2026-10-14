using GooseWebsite.Api.Modules.Accounts.Domain;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>Who is asking the persistence module to read or write.</summary>
public enum CallerKind
{
    /// <summary>Code running outside a web request: startup, migrations, the operator commands.</summary>
    System,

    /// <summary>A web request with no signed-in user.</summary>
    Anonymous,

    /// <summary>A web request from a signed-in user who is not an administrator (a Reader).</summary>
    Authenticated,

    /// <summary>A web request from a signed-in Site administrator.</summary>
    Administrator
}

/// <summary>
/// The caller of the current unit of work. Modules never pass identity by hand: for a web request it is the
/// signed-in user from the session cookie; everything else is the system.
/// </summary>
public interface ICurrentCaller
{
    CallerKind Kind { get; }

    /// <summary>True when the work belongs to a web request, so its changes are committed or rolled back together.</summary>
    bool InHttpRequest => false;
}

/// <summary>The caller when nothing says otherwise (no web request).</summary>
internal sealed class SystemCaller : ICurrentCaller
{
    public static readonly SystemCaller Instance = new();

    public CallerKind Kind => CallerKind.System;
}

/// <summary>Reads the caller from the current web request.</summary>
public sealed class HttpCurrentCaller(IHttpContextAccessor accessor) : ICurrentCaller
{
    public bool InHttpRequest => accessor.HttpContext is not null;

    public CallerKind Kind
    {
        get
        {
            var http = accessor.HttpContext;
            if (http is null)
            {
                return CallerKind.System;
            }

            if (http.User.Identity?.IsAuthenticated != true)
            {
                return CallerKind.Anonymous;
            }

            return http.User.IsInRole(AccountRoles.Admin) ? CallerKind.Administrator : CallerKind.Authenticated;
        }
    }
}
