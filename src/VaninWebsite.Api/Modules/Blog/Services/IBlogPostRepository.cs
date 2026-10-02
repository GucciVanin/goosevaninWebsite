using VaninWebsite.Api.Modules.Blog.Domain;

namespace VaninWebsite.Api.Modules.Blog.Services;

// Keeps blog persistence behind one seam so controllers never touch the database context.
public interface IBlogPostRepository
{
    Task<IReadOnlyList<BlogPost>> GetPublishedAsync(string? search, string? category, int page, int pageSize);
    Task<BlogPost?> GetPublishedBySlugAsync(string slug);
    Task<BlogPost?> GetByIdWithAuthorAsync(int id);
    Task<BlogPost?> FindByIdAsync(int id);
    Task<bool> SlugExistsAsync(string slug);
    void Add(BlogPost post);
    void Remove(BlogPost post);
    Task SaveChangesAsync();
}
