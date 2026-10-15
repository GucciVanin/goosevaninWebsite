using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Projects.Contracts;
using GooseWebsite.Api.Modules.Projects.Services;

namespace GooseWebsite.Api.Modules.Projects.Controllers;

// Public reads of published projects. Everything that changes projects lives in ProjectEditorController.
[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(IProjectRepository projects) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> List() =>
        Ok((await projects.GetPublishedAsync()).Select(ProjectMapping.ToResponse).ToList());

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProjectResponse>> Get(string slug)
    {
        var project = await projects.GetPublishedBySlugAsync(slug);
        return project is null ? NotFound() : Ok(ProjectMapping.ToResponse(project));
    }
}
