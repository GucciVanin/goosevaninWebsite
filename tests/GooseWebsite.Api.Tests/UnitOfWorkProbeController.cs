using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Accounts.Domain;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Tests;

// Only added to the host by tests (AuthApiFactory.includeProbeControllers). Each endpoint writes something and then
// ends in a different way, so the tests can check that one click either persists completely or not at all.
[ApiController]
[Route("test-uow")]
public sealed class UnitOfWorkProbeController(DatabaseService database) : ControllerBase
{
    [HttpPost("commit")]
    public async Task<IActionResult> Commit()
    {
        await CreateUserAsync("uow-commit");
        return Ok();
    }

    [HttpPost("throws")]
    public async Task<IActionResult> Throws()
    {
        await CreateUserAsync("uow-throws");
        throw new InvalidOperationException("the click failed after writing");
    }

    // A sign-in that fails still has to count its attempt: ending with a 401 is not a failure to roll back.
    [HttpPost("unauthorized")]
    public async Task<IActionResult> RefusedButSaved()
    {
        await CreateUserAsync("uow-unauthorized");
        return Unauthorized();
    }

    // The first step saves, the second is refused by the access rules: the first must not survive.
    [HttpPost("second-step-refused")]
    public async Task<IActionResult> SecondStepRefused()
    {
        await CreateUserAsync("uow-first-step");
        database.Add(new BlogPost { Title = "never stored", Slug = "never-stored", AuthorId = "none" });
        await database.SaveChangesAsync();
        return Ok();
    }

    // The action catches the refusal and answers 400. The click still failed, so the earlier step must not survive.
    [HttpPost("swallowed-refusal")]
    public async Task<IActionResult> SwallowedRefusal()
    {
        await CreateUserAsync("uow-swallowed");
        try
        {
            database.Add(new BlogPost { Title = "never stored", Slug = "never-stored-2", AuthorId = "none" });
            await database.SaveChangesAsync();
        }
        catch (DataAccessDeniedException)
        {
            return BadRequest();
        }

        return Ok();
    }

    private async Task CreateUserAsync(string name)
    {
        var user = new ApplicationUser { UserName = name, Email = $"{name}@example.test", DisplayName = name };
        var result = await database.CreateUserAsync(user, "Strong-password-123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("probe user could not be created");
        }
    }
}
