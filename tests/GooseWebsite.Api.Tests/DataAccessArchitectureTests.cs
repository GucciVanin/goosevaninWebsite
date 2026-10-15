using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Shared.Persistence;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F7-US1 acceptance test 4 and Gustavo's decision that every entity declares its own access rule.
public sealed class DataAccessArchitectureTests
{
    [Fact]
    public async Task EveryEntityInTheModelDeclaresAnAccessRule()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

        var withoutRule = context.Model.GetEntityTypes()
            .Select(entity => entity.ClrType)
            .Where(type => AccessRuleCatalog.RuleFor(type) is null)
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            withoutRule.Count == 0,
            $"These entities declare no access rule (add an EntityAccessRule<T> next to their configuration): {string.Join(", ", withoutRule)}");
    }

    [Fact]
    public void NoModuleTouchesTheDatabaseContextOrTheIdentityStoresDirectly()
    {
        var apiRoot = Path.Combine(FindRepositoryRoot(), "src", "GooseWebsite.Api");
        var forbidden = new[] { "ApplicationDbContext", "UserManager<", "RoleManager<", "IgnoreQueryFilters", "ExecuteUpdate", "ExecuteDelete", "FromSql", "ExecuteSql" };
        var allowedPersistence = Path.Combine(apiRoot, "Shared", "Persistence") + Path.DirectorySeparatorChar;
        var migrations = Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar;
        // Registering Identity's EF store is the one allowed mention outside the persistence module.
        var identityRegistration = Path.Combine(apiRoot, "Modules", "Accounts", "AccountsModule.cs");

        var offenders = Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                && !file.Contains(migrations)
                && !file.StartsWith(allowedPersistence, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(file, identityRegistration, StringComparison.OrdinalIgnoreCase))
            .SelectMany(file => forbidden.Where(File.ReadAllText(file).Contains).Select(name => $"{Path.GetRelativePath(apiRoot, file)} mentions {name}"))
            .ToList();

        Assert.True(offenders.Count == 0, "Go through DatabaseService instead:\n" + string.Join('\n', offenders));
    }

    // Rule 5 and F4-US2: no write endpoint ships without saying who may call it, and a signed-in-only write is
    // protected against CSRF. A future projects or images endpoint cannot be left open by forgetting an attribute.
    [Fact]
    public void EveryWriteActionDeclaresWhoMayCallIt_AndProtectedWritesRequireTheAntiForgeryToken()
    {
        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(type));

        var problems = WriteActionProblems(controllers);

        Assert.True(problems.Count == 0, string.Join('\n', problems));
    }

    // The check above must really see inherited actions and every way of declaring a write verb.
    [Fact]
    public void TheWriteActionCheckSeesInheritedActionsAndAcceptVerbsAndTheAntiForgeryRule()
    {
        var problems = WriteActionProblems([typeof(InheritsAnOpenWrite), typeof(UsesAcceptVerbs), typeof(SignedInWithoutToken), typeof(ProperlyProtected)]);

        Assert.Contains(problems, problem => problem.Contains("InheritsAnOpenWrite.OpenInBase"));
        Assert.Contains(problems, problem => problem.Contains("UsesAcceptVerbs.OpenViaAcceptVerbs"));
        Assert.Contains(problems, problem => problem.Contains("SignedInWithoutToken.Change") && problem.Contains("ValidateAntiForgeryToken"));
        Assert.DoesNotContain(problems, problem => problem.Contains("ProperlyProtected"));
        Assert.Equal(3, problems.Count);
    }

    private static List<string> WriteActionProblems(IEnumerable<Type> controllers)
    {
        var safeMethods = new[] { "GET", "HEAD", "OPTIONS" };
        var problems = new List<string>();

        foreach (var controller in controllers)
        {
            var controllerAuthorizes = controller.IsDefined(typeof(AuthorizeAttribute), inherit: true);
            var controllerAllowsAnonymous = controller.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
            var controllerValidatesToken = controller.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), inherit: true);

            // Public instance methods including those inherited from an application base controller (not the framework's).
            var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.DeclaringType is { Namespace: not null } declaring
                    && !declaring.Namespace.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.Ordinal)
                    && declaring != typeof(object)
                    && !method.IsDefined(typeof(NonActionAttribute), inherit: true));

            foreach (var action in actions)
            {
                var verbs = action.GetCustomAttributes(inherit: true).OfType<IActionHttpMethodProvider>().SelectMany(provider => provider.HttpMethods);
                if (!verbs.Any(verb => !safeMethods.Contains(verb, StringComparer.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var name = $"{controller.Name}.{action.Name}";
                var allowsAnonymous = controllerAllowsAnonymous || action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
                var authorizes = controllerAuthorizes || action.IsDefined(typeof(AuthorizeAttribute), inherit: true);
                var validatesToken = controllerValidatesToken || action.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), inherit: true);

                if (!allowsAnonymous && !authorizes)
                {
                    problems.Add($"{name} is a write action with neither [Authorize] nor [AllowAnonymous]");
                }
                else if (authorizes && !allowsAnonymous && !validatesToken)
                {
                    problems.Add($"{name} requires sign-in but lacks [ValidateAntiForgeryToken]");
                }
            }
        }

        return problems;
    }

    // Deliberately flawed (and one correct) controllers that prove the check above notices what it should.
    private abstract class OpenBaseController : ControllerBase
    {
        [HttpPost("base")]
        public IActionResult OpenInBase() => Ok();
    }

    private sealed class InheritsAnOpenWrite : OpenBaseController;

    private sealed class UsesAcceptVerbs : ControllerBase
    {
        [AcceptVerbs("POST")]
        public IActionResult OpenViaAcceptVerbs() => Ok();
    }

    private sealed class SignedInWithoutToken : ControllerBase
    {
        [Authorize]
        [HttpPut("x")]
        public IActionResult Change() => Ok();
    }

    private sealed class ProperlyProtected : ControllerBase
    {
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpDelete("x")]
        public IActionResult Remove() => Ok();

        [HttpGet("read")]
        public IActionResult Read() => Ok();
    }
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GooseWebsite.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("GooseWebsite.slnx was not found above the test output.");
    }
}
