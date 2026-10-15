using Microsoft.EntityFrameworkCore;
using GooseWebsite.Api.Modules.Projects.Domain;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Projects.Services;

// Holds the project queries; every read and write goes through the central DatabaseService.
public sealed class ProjectRepository(DatabaseService database) : IProjectRepository
{
    public async Task<IReadOnlyList<Project>> GetPublishedAsync() =>
        await database.Read<Project>()
            .Where(project => project.IsPublished)
            .OrderByDescending(project => project.PublishedAtUtc ?? project.CreatedAtUtc)
            .ToListAsync();

    public Task<Project?> GetPublishedBySlugAsync(string slug) =>
        database.Read<Project>().SingleOrDefaultAsync(project => project.Slug == slug && project.IsPublished);

    // Every project, drafts included (the read filter shows drafts to administrators only), most recently changed first.
    public async Task<IReadOnlyList<Project>> GetAllForEditorAsync() =>
        await database.Read<Project>().OrderByDescending(project => project.UpdatedAtUtc).ToListAsync();

    public Task<Project?> GetByIdAsync(int id) =>
        database.ReadForUpdate<Project>().SingleOrDefaultAsync(project => project.Id == id);

    public Task<bool> SlugExistsAsync(string slug) => database.Read<Project>().AnyAsync(project => project.Slug == slug);

    public void Add(Project project) => database.Add(project);

    public void Remove(Project project) => database.Remove(project);

    public Task SaveChangesAsync() => database.SaveChangesAsync();
}
