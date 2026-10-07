using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GooseWebsite.Api.Modules.Accounts.Authorization;
using GooseWebsite.Api.Modules.Blog.Contracts;
using GooseWebsite.Api.Modules.Blog.Domain;
using GooseWebsite.Api.Modules.Blog.Services;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Blog.Controllers;

// Public reads of published posts; writes are limited to provisioned administrators.
[ApiController]
[Route("api/blog")]
public sealed class BlogController(
    IBlogPostRepository posts,
    DatabaseService database) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlogPostResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var results = await posts.GetPublishedAsync(search, category, page, pageSize);
        return Ok(results.Select(ToResponse).ToList());
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<BlogPostResponse>> Get(string slug)
    {
        var post = await posts.GetPublishedBySlugAsync(slug);
        return post is null ? NotFound() : Ok(ToResponse(post));
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult<BlogPostResponse>> Create(BlogPostRequest request)
    {
        var user = await database.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var post = new BlogPost
        {
            Slug = await BlogSlugGenerator.CreateUniqueAsync(request.Title, posts),
            AuthorId = user.Id
        };
        ApplyRequestFields(post, request);
        posts.Add(post);
        await posts.SaveChangesAsync();
        post.Author = user;
        return CreatedAtAction(nameof(Get), new { slug = post.Slug }, ToResponse(post));
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ValidateAntiForgeryToken]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BlogPostResponse>> Update(int id, BlogPostRequest request)
    {
        var post = await posts.GetByIdWithAuthorAsync(id);
        if (post is null)
        {
            return NotFound();
        }

        ApplyRequestFields(post, request);
        post.UpdatedAtUtc = DateTime.UtcNow;
        await posts.SaveChangesAsync();
        return Ok(ToResponse(post));
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ValidateAntiForgeryToken]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await posts.FindByIdAsync(id);
        if (post is null)
        {
            return NotFound();
        }

        posts.Remove(post);
        await posts.SaveChangesAsync();
        return NoContent();
    }

    private static void ApplyRequestFields(BlogPost post, BlogPostRequest request)
    {
        post.Title = request.Title.Trim();
        post.Excerpt = request.Excerpt.Trim();
        post.Content = request.Content;
        post.Category = request.Category.Trim();
        post.Tags = string.Join(',', request.Tags ?? []);
        post.IsPublished = request.IsPublished;
    }

    private static BlogPostResponse ToResponse(BlogPost post) => new(
        post.Id,
        post.Title,
        post.Slug,
        post.Excerpt,
        post.Content,
        post.Category,
        post.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries),
        post.IsPublished,
        post.CreatedAtUtc,
        post.UpdatedAtUtc,
        post.AuthorId,
        post.Author.DisplayName);
}
