using System.Linq.Expressions;
using System.Reflection;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>Who may add, change or delete the rows of an entity.</summary>
public enum WriteAccess
{
    /// <summary>
    /// Any caller. For entities only the Identity managers write (users, roles, claims), including for anonymous
    /// visitors who register or fail a sign-in. Modules cannot reach the database except through the gateway.
    /// </summary>
    Anyone,

    /// <summary>Site administrators (and the system).</summary>
    Administrator,

    /// <summary>Only code running outside a web request.</summary>
    System
}

/// <summary>Text the user typed into an entity: normalized on save, and optionally limited in length.</summary>
public sealed record UserTextRule(PropertyInfo Property, int? MaxLength);

/// <summary>The access rule of one entity, declared next to its mapping (see <see cref="EntityAccessRule{T}"/>).</summary>
public interface IEntityAccessRule
{
    Type EntityType { get; }

    WriteAccess Writes { get; }

    /// <summary>Rows everybody may read; administrators and the system read every row. Null means every row is public.</summary>
    LambdaExpression? VisibleToEveryone { get; }

    IReadOnlyList<UserTextRule> UserText { get; }
}

/// <summary>
/// Declare one subclass per entity, in the module that owns it. The persistence module discovers it, so a rule
/// cannot be bypassed and a new entity without a rule fails an architecture test.
/// </summary>
public abstract class EntityAccessRule<T> : IEntityAccessRule where T : class
{
    private readonly List<UserTextRule> _userText = [];

    protected EntityAccessRule(WriteAccess writes) => Writes = writes;

    public Type EntityType => typeof(T);

    public WriteAccess Writes { get; }

    public LambdaExpression? VisibleToEveryone { get; private set; }

    public IReadOnlyList<UserTextRule> UserText => _userText;

    /// <summary>Everybody reads only the rows matching <paramref name="predicate"/>; administrators read all.</summary>
    protected void VisibleWhen(Expression<Func<T, bool>> predicate) => VisibleToEveryone = predicate;

    /// <summary>
    /// The property holds text the user wrote: it is trimmed, other control characters are stripped (line breaks
    /// and tabs stay), and, when <paramref name="maxLength"/> is given, longer text is rejected.
    /// </summary>
    protected void IsUserText(Expression<Func<T, string?>> property, int? maxLength = null)
    {
        if (property.Body is not MemberExpression { Member: PropertyInfo info })
        {
            throw new ArgumentException("Pass a property, for example post => post.Title.", nameof(property));
        }

        _userText.Add(new UserTextRule(info, maxLength));
    }
}

/// <summary>The rule for the tables ASP.NET Identity owns.</summary>
internal sealed class SystemManagedRule<T>() : EntityAccessRule<T>(WriteAccess.Anyone) where T : class;
