using Microsoft.EntityFrameworkCore;
using VaninWebsite.Api.Modules.Blog.Domain;
using VaninWebsite.Api.Shared.Persistence;

namespace VaninWebsite.Api.Modules.Blog.Services;

// Holds the blog's query logic; every read and write goes through the central DatabaseService.
public sealed class BlogPostRepository(DatabaseService database) : IBlogPostRepository
{
    public async Task<IReadOnlyList<BlogPost>> GetPublishedAsync(string? search, string? category, int page, int pageSize)
    {
        var query = database.Read<BlogPost>().Where(post => post.IsPublished);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(post => post.Title.Contains(search)
                || post.Excerpt.Contains(search)
                || post.Tags.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(post => post.Category == category);
        }

        return await query
            .OrderByDescending(post => post.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(post => post.Author)
            .ToListAsync();
    }

    public Task<BlogPost?> GetPublishedBySlugAsync(string slug) =>
        database.Read<BlogPost>()
            .Include(post => post.Author)
            .SingleOrDefaultAsync(post => post.Slug == slug && post.IsPublished);

    public Task<BlogPost?> GetByIdWithAuthorAsync(int id) =>
        database.ReadForUpdate<BlogPost>().Include(post => post.Author).SingleOrDefaultAsync(post => post.Id == id);

    public Task<BlogPost?> FindByIdAsync(int id) => database.FindAsync<BlogPost>(id);

    public Task<bool> SlugExistsAsync(string slug) => database.Read<BlogPost>().AnyAsync(post => post.Slug == slug);

    public void Add(BlogPost post) => database.Add(post);

    public void Remove(BlogPost post) => database.Remove(post);

    public Task SaveChangesAsync() => database.SaveChangesAsync();
}
