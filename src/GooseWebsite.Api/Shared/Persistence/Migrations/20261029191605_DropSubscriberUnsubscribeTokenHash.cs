using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GooseWebsite.Api.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropSubscriberUnsubscribeTokenHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscribers_UnsubscribeTokenHash",
                table: "Subscribers");

            migrationBuilder.DropColumn(
                name: "UnsubscribeTokenHash",
                table: "Subscribers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnsubscribeTokenHash",
                table: "Subscribers",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Subscribers_UnsubscribeTokenHash",
                table: "Subscribers",
                column: "UnsubscribeTokenHash",
                unique: true);
        }
    }
}
