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
    public async Task<ActionResult<IReadOnlyList<PublicProjectResponse>>> List() =>
        Ok((await projects.GetPublishedAsync()).Select(ProjectMapping.ToPublic).ToList());

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicProjectResponse>> Get(string slug)
    {
        var project = await projects.GetPublishedBySlugAsync(slug);
        return project is null ? NotFound() : Ok(ProjectMapping.ToPublic(project));
    }
}
