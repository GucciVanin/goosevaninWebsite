using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GooseWebsite.Api.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subscribers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false, collation: "NOCASE"),
                    State = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfirmationTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ConfirmationTokenExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UnsubscribeTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscribers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_ConfirmationTokenHash",
                table: "Subscribers",
                column: "ConfirmationTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_Email",
                table: "Subscribers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_UnsubscribeTokenHash",
                table: "Subscribers",
                column: "UnsubscribeTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscribers");
        }
    }
}
