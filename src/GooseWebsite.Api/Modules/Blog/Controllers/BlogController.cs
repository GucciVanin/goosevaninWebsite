using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Blog.Contracts;
using GooseWebsite.Api.Modules.Blog.Services;

namespace GooseWebsite.Api.Modules.Blog.Controllers;

// Public reads of published stories. Everything that changes stories lives in StoryEditorController.
[ApiController]
[Route("api/blog")]
public sealed class BlogController(IBlogPostRepository posts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlogPostResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        page = Math.Max(1, page);
        // No upper bound: this API is internal (Gustavo, 2026-10-28) and the stories index asks for every story.
        pageSize = Math.Max(1, pageSize);
        if ((long)(page - 1) * pageSize > int.MaxValue)
        {
            return Ok(Array.Empty<BlogPostResponse>());
        }

        var results = await posts.GetPublishedAsync(search, category, page, pageSize);
        return Ok(results.Select(BlogPostMapping.ToResponse).ToList());
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<BlogPostResponse>> Get(string slug)
    {
        var post = await posts.GetPublishedBySlugAsync(slug);
        return post is null ? NotFound() : Ok(BlogPostMapping.ToResponse(post));
    }
}
