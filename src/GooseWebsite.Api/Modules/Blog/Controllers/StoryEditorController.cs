using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Accounts.Authorization;
using GooseWebsite.Api.Modules.Blog.Contracts;
using GooseWebsite.Api.Modules.Blog.Services;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Blog.Controllers;

// Gustavo's editor for stories: administrators only, every write needs the CSRF token. The rules (drafts, publishing,
// slugs, tags) live in the authoring service; this controller maps its outcomes to responses.
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/editor/stories")]
public sealed class StoryEditorController(
    IBlogAuthoringService authoring,
    IBlogPostRepository posts,
    DatabaseService database) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StorySummaryResponse>>> List() =>
        Ok((await posts.GetAllForEditorAsync()).Select(BlogPostMapping.ToSummary).ToList());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BlogPostResponse>> Get(int id)
    {
        var post = await posts.GetByIdWithAuthorAsync(id);
        return post is null ? NotFound() : Ok(BlogPostMapping.ToResponse(post));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult<BlogPostResponse>> Create(BlogPostRequest request)
    {
        var user = await database.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await authoring.CreateAsync(request, user.Id);
        return result.Outcome == AuthoringOutcome.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = result.Post!.Id }, BlogPostMapping.ToResponse(result.Post))
            : Refused(result);
    }

    [ValidateAntiForgeryToken]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BlogPostResponse>> Update(int id, BlogPostRequest request) =>
        Answer(await authoring.UpdateAsync(id, request));

    [ValidateAntiForgeryToken]
    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<BlogPostResponse>> Publish(int id) =>
        Answer(await authoring.PublishAsync(id));

    [ValidateAntiForgeryToken]
    [HttpPost("{id:int}/unpublish")]
    public async Task<ActionResult<BlogPostResponse>> Unpublish(int id) =>
        Answer(await authoring.UnpublishAsync(id));

    [ValidateAntiForgeryToken]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await authoring.DeleteAsync(id) ? NoContent() : NotFound();

    private ActionResult<BlogPostResponse> Answer(AuthoringResult result) =>
        result.Outcome == AuthoringOutcome.Succeeded ? Ok(BlogPostMapping.ToResponse(result.Post!)) : Refused(result);

    private ActionResult Refused(AuthoringResult result) => result.Outcome == AuthoringOutcome.NotFound
        ? NotFound()
        : ValidationProblem(new ValidationProblemDetails(result.Errors!.ToDictionary(pair => pair.Key, pair => pair.Value)));
}
