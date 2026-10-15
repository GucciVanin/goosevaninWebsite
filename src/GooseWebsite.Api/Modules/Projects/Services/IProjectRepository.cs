using GooseWebsite.Api.Modules.Projects.Domain;

namespace GooseWebsite.Api.Modules.Projects.Services;

// Keeps project persistence behind one seam so controllers never touch the database context.
public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetPublishedAsync();
    Task<Project?> GetPublishedBySlugAsync(string slug);
    Task<IReadOnlyList<Project>> GetAllForEditorAsync();
    Task<Project?> GetByIdAsync(int id);
    Task<bool> SlugExistsAsync(string slug);
    void Add(Project project);
    void Remove(Project project);
    Task SaveChangesAsync();
}
