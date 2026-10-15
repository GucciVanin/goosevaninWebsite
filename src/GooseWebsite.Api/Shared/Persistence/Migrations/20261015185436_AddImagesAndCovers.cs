using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GooseWebsite.Api.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImagesAndCovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImage",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageAlt",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoverImage",
                table: "BlogPosts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageAlt",
                table: "BlogPosts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ContentImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentImages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentImages_FileName",
                table: "ContentImages",
                column: "FileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentImages");

            migrationBuilder.DropColumn(
                name: "CoverImage",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CoverImageAlt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CoverImage",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "CoverImageAlt",
                table: "BlogPosts");
        }
    }
}
