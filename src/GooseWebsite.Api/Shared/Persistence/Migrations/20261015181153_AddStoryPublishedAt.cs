using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GooseWebsite.Api.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryPublishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "BlogPosts",
                type: "TEXT",
                nullable: true);

            // Stories that were already published keep a publication date: the day they were created.
            migrationBuilder.Sql("UPDATE \"BlogPosts\" SET \"PublishedAtUtc\" = \"CreatedAtUtc\" WHERE \"IsPublished\" = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "BlogPosts");
        }
    }
}
