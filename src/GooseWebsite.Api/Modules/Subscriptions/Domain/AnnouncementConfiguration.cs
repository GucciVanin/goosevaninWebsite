using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Modules.Subscriptions.Domain;

// Discovered by the persistence module. Written inside Gustavo's publish request (an administrator) and by the
// delivery job (the system); nobody else, and no endpoint returns these rows.
public sealed class AnnouncementAccessRule : EntityAccessRule<Announcement>
{
    public const int TitleMaxLength = 200;
    public const int SummaryMaxLength = 600;

    public AnnouncementAccessRule() : base(WriteAccess.Administrator)
    {
        IsUserText(announcement => announcement.Title, TitleMaxLength);
        IsUserText(announcement => announcement.Summary, SummaryMaxLength);
    }
}

public sealed class AnnouncementDeliveryAccessRule : EntityAccessRule<AnnouncementDelivery>
{
    public AnnouncementDeliveryAccessRule() : base(WriteAccess.Administrator)
    {
    }
}

public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");
        builder.Property(announcement => announcement.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(announcement => announcement.Title).HasMaxLength(AnnouncementAccessRule.TitleMaxLength).IsRequired();
        builder.Property(announcement => announcement.Summary).HasMaxLength(AnnouncementAccessRule.SummaryMaxLength).IsRequired();
        builder.Property(announcement => announcement.PublicPath).HasMaxLength(300).IsRequired();

        // One announcement per story or project, whatever happens: the database refuses a second one.
        builder.HasIndex(announcement => new { announcement.Kind, announcement.ContentId }).IsUnique();
        builder.HasIndex(announcement => announcement.CompletedAtUtc);
    }
}

public sealed class AnnouncementDeliveryConfiguration : IEntityTypeConfiguration<AnnouncementDelivery>
{
    public void Configure(EntityTypeBuilder<AnnouncementDelivery> builder)
    {
        builder.ToTable("AnnouncementDeliveries");
        builder.HasOne(delivery => delivery.Announcement)
            .WithMany(announcement => announcement.Deliveries)
            .HasForeignKey(delivery => delivery.AnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);

        // The subscriber link is an id column with a cascading foreign key: when a subscriber unsubscribes, the
        // database removes the owed mail with the record. No navigation, so the module's entities stay independent.
        builder.HasOne<Subscriber>()
            .WithMany()
            .HasForeignKey(delivery => delivery.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);

        // A recipient is owed an announcement at most once.
        builder.HasIndex(delivery => new { delivery.AnnouncementId, delivery.SubscriberId }).IsUnique();
        builder.HasIndex(delivery => delivery.SentAtUtc);
    }
}
