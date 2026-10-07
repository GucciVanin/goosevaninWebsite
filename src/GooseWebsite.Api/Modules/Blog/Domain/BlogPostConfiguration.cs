using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GooseWebsite.Api.Modules.Blog.Domain;

// Discovered by ApplicationDbContext, so the blog owns its table mapping.
public sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        // Pinned: the table was historically named after the DbSet property, so existing databases use "BlogPosts".
        builder.ToTable("BlogPosts");
        builder.HasIndex(post => post.Slug).IsUnique();
        builder.HasOne(post => post.Author)
            .WithMany()
            .HasForeignKey(post => post.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
