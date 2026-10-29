using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GooseWebsite.Api.Modules.Subscriptions.Domain;

// Discovered by the persistence module (the access rule is SubscriberAccessRule).
public sealed class SubscriberConfiguration : IEntityTypeConfiguration<Subscriber>
{
    public void Configure(EntityTypeBuilder<Subscriber> builder)
    {
        builder.ToTable("Subscribers");
        builder.Property(subscriber => subscriber.Name).HasMaxLength(100).IsRequired();

        // NOCASE makes both the unique index and every comparison ignore case: "Ada@x.test" is "ada@x.test".
        builder.Property(subscriber => subscriber.Email).HasMaxLength(254).UseCollation("NOCASE").IsRequired();
        builder.Property(subscriber => subscriber.State).HasConversion<string>().HasMaxLength(16);
        builder.Property(subscriber => subscriber.ConfirmationTokenHash).HasMaxLength(64);

        builder.HasIndex(subscriber => subscriber.Email).IsUnique();
        builder.HasIndex(subscriber => subscriber.ConfirmationTokenHash).IsUnique();
    }
}
