using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Accounts.Authorization;
using GooseWebsite.Api.Modules.Projects.Contracts;
using GooseWebsite.Api.Modules.Projects.Services;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Projects.Controllers;

// Gustavo's editor for projects: administrators only, every write needs the CSRF token. The rules live in the
// authoring service; this controller maps its outcomes to responses.
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/editor/projects")]
public sealed class ProjectEditorController(
    IProjectAuthoringService authoring,
    IProjectRepository projects,
    DatabaseService database) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryResponse>>> List() =>
        Ok((await projects.GetAllForEditorAsync()).Select(ProjectMapping.ToSummary).ToList());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> Get(int id)
    {
        var project = await projects.GetByIdAsync(id);
        return project is null ? NotFound() : Ok(ProjectMapping.ToResponse(project));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(ProjectRequest request)
    {
        var user = await database.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await authoring.CreateAsync(request, user.Id);
        return result.Outcome == ProjectOutcome.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = result.Project!.Id }, ProjectMapping.ToResponse(result.Project))
            : Refused(result);
    }

    [ValidateAntiForgeryToken]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> Update(int id, ProjectRequest request) =>
        Answer(await authoring.UpdateAsync(id, request));

    [ValidateAntiForgeryToken]
    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<ProjectResponse>> Publish(int id) =>
        Answer(await authoring.PublishAsync(id));

    [ValidateAntiForgeryToken]
    [HttpPost("{id:int}/unpublish")]
    public async Task<ActionResult<ProjectResponse>> Unpublish(int id) =>
        Answer(await authoring.UnpublishAsync(id));

    [ValidateAntiForgeryToken]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await authoring.DeleteAsync(id) ? NoContent() : NotFound();

    private ActionResult<ProjectResponse> Answer(ProjectResult result) =>
        result.Outcome == ProjectOutcome.Succeeded ? Ok(ProjectMapping.ToResponse(result.Project!)) : Refused(result);

    private ActionResult Refused(ProjectResult result) => result.Outcome == ProjectOutcome.NotFound
        ? NotFound()
        : ValidationProblem(new ValidationProblemDetails(result.Errors!.ToDictionary(pair => pair.Key, pair => pair.Value)));
}
