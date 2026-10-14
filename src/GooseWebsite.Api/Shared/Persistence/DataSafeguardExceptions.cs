namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// The caller is not allowed to make this change. The message is deliberately generic: it never says which
/// values were involved. The entity name is for logs and tests only.
/// </summary>
public sealed class DataAccessDeniedException(string entityName) : Exception("The operation is not allowed.")
{
    public string EntityName { get; } = entityName;
}

/// <summary>A value could not be accepted (for example it is longer than its declared limit). Nothing was saved.</summary>
public sealed class DataRejectedException(string entityName, string propertyName) : Exception("Some of the text could not be accepted.")
{
    public string EntityName { get; } = entityName;

    public string PropertyName { get; } = propertyName;
}
