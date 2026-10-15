using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GooseWebsite.Api.Modules.Projects.Domain;

// Discovered by the persistence module, so the Projects module owns its table mapping.
public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasIndex(project => project.Slug).IsUnique();
        builder.HasOne(project => project.Author)
            .WithMany()
            .HasForeignKey(project => project.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
