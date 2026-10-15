using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GooseWebsite.Api.Modules.Media.Domain;

public sealed class ContentImageConfiguration : IEntityTypeConfiguration<ContentImage>
{
    public void Configure(EntityTypeBuilder<ContentImage> builder)
    {
        builder.ToTable("ContentImages");
        builder.HasIndex(image => image.FileName).IsUnique();
    }
}
