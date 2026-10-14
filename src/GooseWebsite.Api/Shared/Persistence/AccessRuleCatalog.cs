using Microsoft.AspNetCore.Identity;

namespace GooseWebsite.Api.Shared.Persistence;

/// <summary>
/// Every entity's access rule: the ones modules declare (found by scanning the assembly, like the entity
/// configurations) plus the Identity tables. Built once.
/// </summary>
public static class AccessRuleCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<Type, IEntityAccessRule>> Rules = new(Build);

    public static IReadOnlyCollection<IEntityAccessRule> All => Rules.Value.Values.ToList();

    public static IEntityAccessRule? RuleFor(Type entityType) => Rules.Value.GetValueOrDefault(entityType);

    private static Dictionary<Type, IEntityAccessRule> Build()
    {
        var rules = new Dictionary<Type, IEntityAccessRule>();

        IEntityAccessRule[] identityTables =
        [
            new SystemManagedRule<IdentityRole>(),
            new SystemManagedRule<IdentityRoleClaim<string>>(),
            new SystemManagedRule<IdentityUserRole<string>>(),
            new SystemManagedRule<IdentityUserClaim<string>>(),
            new SystemManagedRule<IdentityUserLogin<string>>(),
            new SystemManagedRule<IdentityUserToken<string>>()
        ];
        foreach (var rule in identityTables)
        {
            Add(rules, rule);
        }

        var declared = typeof(AccessRuleCatalog).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false }
                && typeof(IEntityAccessRule).IsAssignableFrom(type)
                && type != typeof(SystemManagedRule<>)
                && !Array.Exists(identityTables, table => table.GetType() == type)
                && type.GetConstructor(Type.EmptyTypes) is not null);
        foreach (var type in declared)
        {
            Add(rules, (IEntityAccessRule)Activator.CreateInstance(type)!);
        }

        return rules;
    }

    private static void Add(Dictionary<Type, IEntityAccessRule> rules, IEntityAccessRule rule)
    {
        if (!rules.TryAdd(rule.EntityType, rule))
        {
            throw new InvalidOperationException($"{rule.EntityType.Name} declares more than one access rule.");
        }
    }
}
