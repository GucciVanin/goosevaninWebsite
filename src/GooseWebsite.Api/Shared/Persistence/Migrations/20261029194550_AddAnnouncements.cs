using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GooseWebsite.Api.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnnouncedAtUtc",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnnouncedAtUtc",
                table: "BlogPosts",
                type: "TEXT",
                nullable: true);

            // Everything published before announcements existed counts as announced, so that unpublishing and
            // publishing an old story or project again never mails subscribers about work they could already see.
            // That includes work that was published once and is a draft now (it keeps its publication date).
            migrationBuilder.Sql(
                "UPDATE BlogPosts SET AnnouncedAtUtc = COALESCE(PublishedAtUtc, CreatedAtUtc) WHERE IsPublished = 1 OR PublishedAtUtc IS NOT NULL;");
            migrationBuilder.Sql(
                "UPDATE Projects SET AnnouncedAtUtc = COALESCE(PublishedAtUtc, CreatedAtUtc) WHERE IsPublished = 1 OR PublishedAtUtc IS NOT NULL;");

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    PublicPath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AnnouncementId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubscriberId = table.Column<int>(type: "INTEGER", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnouncementDeliveries_Announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "Announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnnouncementDeliveries_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "Subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementDeliveries_AnnouncementId_SubscriberId",
                table: "AnnouncementDeliveries",
                columns: new[] { "AnnouncementId", "SubscriberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementDeliveries_SentAtUtc",
                table: "AnnouncementDeliveries",
                column: "SentAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementDeliveries_SubscriberId",
                table: "AnnouncementDeliveries",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CompletedAtUtc",
                table: "Announcements",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_Kind_ContentId",
                table: "Announcements",
                columns: new[] { "Kind", "ContentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnouncementDeliveries");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropColumn(
                name: "AnnouncedAtUtc",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AnnouncedAtUtc",
                table: "BlogPosts");
        }
    }
}
